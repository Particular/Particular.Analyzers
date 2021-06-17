namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UtcNowAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.UseUtcNow);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.SimpleMemberAccessExpression);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            if (memberAccess.Name.ToString() != "Now")
            {
                return;
            }

            if (!(memberAccess.Expression is IdentifierNameSyntax identifier))
            {
                return;
            }

            var value = identifier?.Identifier.ValueText;

            if (value == "DateTime" || value == "DateTimeOffset")
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.UseUtcNow, context.Node.GetLocation(), value);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
