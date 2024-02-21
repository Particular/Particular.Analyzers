namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyTokenAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.EmptyCancellationTokenDefaultLiteral,
            DiagnosticDescriptors.EmptyCancellationTokenDefaultOperator);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Argument);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ArgumentSyntax arg)
            {
                return;
            }

            // These assignments and the conditional could be optimized to short-circuit
            // but it ends up being much less readable,
            // and these operations are cheap, so it's not worth it.
            // Note that although CSharpSyntaxNode.Kind() is a method, the implementation call stack ends up just returning a field reference.
            var defaultLiteral = (arg.Expression as LiteralExpressionSyntax)?.Kind() == SyntaxKind.DefaultLiteralExpression;
            var defaultOperator = arg.Expression is DefaultExpressionSyntax;

            if (!defaultLiteral && !defaultOperator)
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken).Type;

            if (!type.IsCancellationToken())
            {
                return;
            }

            var descriptor = defaultLiteral
                ? DiagnosticDescriptors.EmptyCancellationTokenDefaultLiteral
                : DiagnosticDescriptors.EmptyCancellationTokenDefaultOperator;

            context.ReportDiagnostic(descriptor, arg);
        }
    }
}
