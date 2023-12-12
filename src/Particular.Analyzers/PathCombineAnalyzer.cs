namespace Particular.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PathCombineAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.ImproperPathCombine);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(Analyze);
        }

        static void Analyze(CompilationStartAnalysisContext startContext)
        {
            var knownTypes = new KnownTypes(startContext.Compilation);

            if (!knownTypes.IsValid())
            {
                return;
            }

            startContext.RegisterSyntaxNodeAction(context => AnalyzeInvocation(context, knownTypes), SyntaxKind.InvocationExpression);
        }

        static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            if (!(memberAccess.Expression is IdentifierNameSyntax classNameSyntax) || classNameSyntax.Identifier.Text != "Path")
            {
                return;
            }

            if (memberAccess.Name.Identifier.Text != "Combine")
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(invocation.Expression, context.CancellationToken).Symbol?.GetMethodOrDefault() is IMethodSymbol method))
            {
                return;
            }

            if (!method?.ContainingType.Equals(knownTypes.SystemIOPath, SymbolEqualityComparer.Default) ?? false)
            {
                return;
            }

            if (method?.ContainingType.Name != "Path" || method.Name != "Combine")
            {
                return;
            }

            var arguments = invocation.ArgumentList.Arguments;
            for (var i = 1; i < arguments.Count; i++) // Skip first argument
            {
                var expression = arguments[i].Expression;
                var diagnostic = GetDiagnostic(context, expression);
                if (diagnostic != null)
                {
                    context.ReportDiagnostic(diagnostic, expression);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "There's a TON of SyntaxKind values and we only care about a few")]
        static DiagnosticDescriptor GetDiagnostic(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            switch (expression.Kind())
            {
                case SyntaxKind.StringLiteralExpression:
                    var literalValue = (expression as LiteralExpressionSyntax).Token.Value as string;
                    return HasSeparatorChars(literalValue) ? DiagnosticDescriptors.ImproperPathCombine : null;

                case SyntaxKind.InterpolatedStringExpression:
                    return EvaluateInterpolatedString(expression as InterpolatedStringExpressionSyntax);

                case SyntaxKind.IdentifierName:
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                    return EvaluatePotentialStringExpression(context, expression);

                default:
                    return null;
            }
        }

        static DiagnosticDescriptor EvaluateInterpolatedString(InterpolatedStringExpressionSyntax expression)
        {
            foreach (var textSyntax in expression.Contents.OfType<InterpolatedStringTextSyntax>())
            {
                if (HasSeparatorChars(textSyntax.TextToken.Value as string))
                {
                    return DiagnosticDescriptors.ImproperPathCombine;
                }
            }
            foreach (var interpSyntax in expression.Contents.OfType<InterpolationSyntax>())
            {
                if (interpSyntax != null)
                {
                    return DiagnosticDescriptors.ImproperPathCombine;
                }
            }
            return null;
        }

        static DiagnosticDescriptor EvaluatePotentialStringExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol;

            if (symbol is ILocalSymbol localSymbol && localSymbol.Type.SpecialType == SpecialType.System_String)
            {
                return DiagnosticDescriptors.ImproperPathCombine;
            }

            if (symbol is IMethodSymbol methodSymbol && methodSymbol.ReturnType.SpecialType == SpecialType.System_String)
            {
                return DiagnosticDescriptors.ImproperPathCombine;
            }

            if (symbol is IPropertySymbol propSymbol && propSymbol.Type.SpecialType == SpecialType.System_String)
            {
                return DiagnosticDescriptors.ImproperPathCombine;
            }

            return null;
        }

        static readonly char[] pathSeparatorChars = new[] { '/', '\\' };

        static bool HasSeparatorChars(string value) => value?.IndexOfAny(pathSeparatorChars) >= 0;

        class KnownTypes
        {
            public INamedTypeSymbol SystemIOPath { get; }

            public KnownTypes(Compilation compilation)
            {
                SystemIOPath = compilation.GetTypeByMetadataName("System.IO.Path");
            }

            public bool IsValid() =>
                SystemIOPath != null;
        }
    }
}
