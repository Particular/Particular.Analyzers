namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ContextMethodParameterAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.CancellableContextMethodCancellationToken];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                Analyze,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.StructDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not TypeDeclarationSyntax type)
            {
                return;
            }

            // cheapest checks first
            if (!type.BaseList?.Types.Any() ?? false)
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(type, context.CancellationToken) is INamedTypeSymbol symbol)
            {
                Analyze(context, symbol);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, INamedTypeSymbol type)
        {
            if (!type.IsCancellableContext())
            {
                return;
            }

            foreach (var member in type.GetMembers())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (member is not IMethodSymbol method)
                {
                    continue;
                }

                Analyze(context, method);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IMethodSymbol method)
        {
            // cheapest checks first
            if (method.IsStatic)
            {
                return;
            }

            if (method.MethodKind == MethodKind.Constructor)
            {
                return;
            }

            foreach (var param in method.Parameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Analyze(context, param);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IParameterSymbol param)
        {
            if (!param.Type.IsCancellationToken())
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.CancellableContextMethodCancellationToken, param);
        }
    }
}
