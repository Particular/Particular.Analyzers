namespace Particular.CodeRules.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.CodeRules.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyTokenAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.EmptyCancellationTokenDefaultLiteral,
            DiagnosticDescriptors.EmptyCancellationTokenDefaultOperator,
            DiagnosticDescriptors.EmptyCancellationTokenNone);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Argument);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ArgumentSyntax arg))
            {
                return;
            }

            // These assignments and the conditional could be optimized to short-circuit
            // but it ends up being much less readable,
            // and these operations are cheap, so it's not worth it.
            // Note that although CSharpSyntaxNode.Kind() is a method, the implementation call stack ends up just returning a field reference.
            var defaultLiteral = (arg.Expression as LiteralExpressionSyntax)?.Kind() == SyntaxKind.DefaultLiteralExpression;

            var defaultOperator = arg.Expression is DefaultExpressionSyntax;

            var cancellationTokenNone =
                arg.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "None" &&
                memberAccess.Expression is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == "CancellationToken";

            if (!defaultLiteral && !defaultOperator && !cancellationTokenNone)
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
                : defaultOperator
                    ? DiagnosticDescriptors.EmptyCancellationTokenDefaultOperator
                    : DiagnosticDescriptors.EmptyCancellationTokenNone;

            context.ReportDiagnostic(descriptor, arg);
        }
    }
}
