namespace Particular.Analyzers.Extensions
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    static class SyntaxNodeAnalysisContextExtension
    {
        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                context.ReportDiagnostic(descriptor, location);
            }
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode node) =>
            context.ReportDiagnostic(descriptor, node.GetLocation());

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, Location location) =>
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
    }
}
