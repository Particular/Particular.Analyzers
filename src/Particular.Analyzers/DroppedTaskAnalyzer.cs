namespace Particular.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DroppedTaskAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.DroppedTask);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            // cheapest checks first
            if (invocation.Parent is not ExpressionStatementSyntax)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation.Expression, context.CancellationToken).Symbol is ISymbol symbol)
            {
                Analyze(context, symbol, invocation);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, ISymbol expression, InvocationExpressionSyntax invocation)
        {
            if (expression.GetMethodOrDefault() is not IMethodSymbol method)
            {
                return;
            }

            if (!method.ReturnType.IsTask() && !method.ReturnType.IsConfiguredTaskAwaitable())
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.DroppedTask, invocation);
        }
    }
}
