namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeNowAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.NowUsedInsteadOfUtcNow);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (memberAccess.Name.ToString() != "Now")
            {
                return;
            }

            if (memberAccess.Expression is not IdentifierNameSyntax identifier)
            {
                return;
            }

            var value = identifier?.Identifier.ValueText;

            if (value is "DateTime" or "DateTimeOffset")
            {
                context.ReportDiagnostic(DiagnosticDescriptors.NowUsedInsteadOfUtcNow, memberAccess, value);
            }
        }
    }
}
