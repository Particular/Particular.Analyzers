namespace Particular.Analyzers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DictionaryKeysAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DictionaryHasUnsupportedKeyType);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(StartAnalysis);
        }

        void StartAnalysis(CompilationStartAnalysisContext context)
        {
            var knownTypes = new KnownTypes(context.Compilation);

            context.RegisterSymbolAction(c => AnalyzeProperty(c, knownTypes), SymbolKind.Property);
            context.RegisterSymbolAction(c => AnalyzeField(c, knownTypes), SymbolKind.Field);
            context.RegisterSymbolAction(c => AnalyzeMethod(c, knownTypes), SymbolKind.Method);

            context.RegisterSyntaxNodeAction(c => AnalyzeVariableDeclaration(c, knownTypes), SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(c => AnalyzeClassDeclaration(c, knownTypes), SyntaxKind.ClassDeclaration);
        }

        void AnalyzeProperty(SymbolAnalysisContext context, KnownTypes knownTypes)
        {
            if (context.Symbol is IPropertySymbol prop)
            {
                var typeSyntax = GetNearest<PropertyDeclarationSyntax>(prop, context.CancellationToken)?.Type;
                AnalyzeType(knownTypes, prop.Type, typeSyntax, context.ReportDiagnostic);
            }
        }

        void AnalyzeField(SymbolAnalysisContext context, KnownTypes knownTypes)
        {
            if (context.Symbol is IFieldSymbol field)
            {
                var typeSyntax = GetNearest<VariableDeclarationSyntax>(field, context.CancellationToken)?.Type;
                AnalyzeType(knownTypes, field.Type, typeSyntax, context.ReportDiagnostic);
            }
        }

        void AnalyzeMethod(SymbolAnalysisContext context, KnownTypes knownTypes)
        {
            if (context.Symbol is IMethodSymbol method)
            {
                if (method.AssociatedSymbol is IPropertySymbol)
                {
                    // The getter of a property is tested as a property, not as a method
                    return;
                }

                // Will be null for things like operator overloads
                var methodSyntax = GetNearest<MethodDeclarationSyntax>(method, context.CancellationToken);

                if (!method.ReturnsVoid && methodSyntax != null)
                {
                    AnalyzeType(knownTypes, method.ReturnType, methodSyntax.ReturnType, context.ReportDiagnostic);
                }

                for (var i = 0; i < method.Parameters.Length; i++)
                {
                    var parameterType = method.Parameters[i].Type;
                    var parameterSyntax = GetNearest<ParameterSyntax>(method.Parameters[i], context.CancellationToken)?.Type;
                    if (parameterSyntax != null)
                    {
                        // Syntax will be null for a parameter when analyzing a top-level statements class
                        AnalyzeType(knownTypes, parameterType, parameterSyntax, context.ReportDiagnostic);
                    }
                }
            }
        }

        void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            if (context.Node is LocalDeclarationStatementSyntax vDec)
            {
                // Don't want to analyze "var"
                if (vDec.Declaration.Type is GenericNameSyntax nameSyntax)
                {
                    var typeSymbol = context.SemanticModel.GetTypeInfo(vDec.Declaration.Type);
                    AnalyzeType(knownTypes, typeSymbol.Type, nameSyntax, context.ReportDiagnostic);
                }

                foreach (var variable in vDec.Declaration.Variables)
                {
                    if (variable.Initializer?.Value is ObjectCreationExpressionSyntax creationSyntax)
                    {
                        // Don't want to analyze "null" or another variable name or new()
                        if (creationSyntax.Type != null)
                        {
                            if (context.SemanticModel.GetSymbolInfo(creationSyntax.Type, context.CancellationToken).Symbol is INamedTypeSymbol typeSymbol)
                            {
                                AnalyzeType(knownTypes, typeSymbol, creationSyntax.Type, context.ReportDiagnostic);
                            }
                        }
                    }
                }
            }
        }

        void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            if (context.Node is ClassDeclarationSyntax classDec)
            {
                if (classDec.BaseList != null && context.SemanticModel.GetDeclaredSymbol(classDec, context.CancellationToken) is ITypeSymbol classType)
                {
                    foreach (var baseType in classDec.BaseList.Types)
                    {
                        var type = context.SemanticModel.GetTypeInfo(baseType.Type, context.CancellationToken).Type;
                        if (type != null)
                        {
                            AnalyzeType(knownTypes, type, baseType, context.ReportDiagnostic);
                        }
                    }
                }
            }
        }

        TSyntaxType GetNearest<TSyntaxType>(ISymbol symbol, CancellationToken cancellationToken)
            where TSyntaxType : SyntaxNode
        {
            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                return null;
            }

            var firstReference = symbol.DeclaringSyntaxReferences[0];
            var declaringSyntax = firstReference.GetSyntax(cancellationToken);
            var syntax = declaringSyntax.FirstAncestorOrSelf<TSyntaxType>();
            return syntax;
        }

        void AnalyzeType(KnownTypes knownTypes, ITypeSymbol type, SyntaxNode syntax, Action<Diagnostic> reportDiagnostic)
        {
            if (syntax is null)
            {
                // Situations like a record class, where the "Property" is expressed like a "Parameter" so we don't find a PropertyDeclarationSyntax
                return;
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                AnalyzeType(knownTypes, arrayType.ElementType, syntax, reportDiagnostic);
                return;
            }

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType && knownTypes.DictionaryTypes.Contains(namedType.ConstructedFrom, SymbolEqualityComparer.IncludeNullability))
            {
                var key = namedType.TypeArguments[0];

                if (!IsAppropriateDictionaryKey(knownTypes, key))
                {
                    var simpleType = namedType.Name.Split('`')[0];
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.DictionaryHasUnsupportedKeyType, syntax.GetLocation(), simpleType, key.ToDisplayString());
                    reportDiagnostic(diagnostic);
                }
            }
        }

        static bool IsAppropriateDictionaryKey(KnownTypes knownTypes, ITypeSymbol type)
        {
            if (type.Equals(knownTypes.String, SymbolEqualityComparer.IncludeNullability) || type.IsValueType)
            {
                return true;
            }

            var implementsIEquatable = type.Interfaces
                .Any(iface => iface.IsGenericType && iface.ConstructedFrom.Equals(knownTypes.IEquatableT, SymbolEqualityComparer.Default));

            if (implementsIEquatable)
            {
                return true;
            }

            bool hasEquals = false;
            bool hasGetHashCode = false;

            foreach (var member in type.GetMembers())
            {
                hasEquals |= member.Name == "Equals" && member.IsOverride;
                hasGetHashCode |= member.Name == "GetHashCode" && member.IsOverride;
            }

            return hasEquals && hasGetHashCode;
        }

        class KnownTypes
        {
            public ImmutableHashSet<INamedTypeSymbol> DictionaryTypes { get; }
            public INamedTypeSymbol String { get; }
            public INamedTypeSymbol IEquatableT { get; }

            public KnownTypes(Compilation compilation)
            {
                var dictionaryTypes = new Type[]
                {
                    typeof(IDictionary<,>),
                    typeof(Dictionary<,>),
                    typeof(IReadOnlyDictionary<,>),
                    typeof(ConcurrentDictionary<,>),
                    typeof(HashSet<>),
                    typeof(ISet<>),
                    typeof(ImmutableHashSet<>),
                    typeof(ImmutableDictionary<,>)
                };

                DictionaryTypes = dictionaryTypes
                    .Select(t => compilation.GetTypeByMetadataName(t.FullName))
                    .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                String = compilation.GetSpecialType(SpecialType.System_String);
                IEquatableT = compilation.GetTypeByMetadataName("System.IEquatable`1");

            }
        }
    }
}
