namespace Particular.Analyzers.Extensions
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    static class SyntaxNodeAnalysisContextExtension
    {
        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params object[] messageArgs)
        {
            foreach (var location in symbol.Locations)
            {
                context.ReportDiagnostic(descriptor, location, messageArgs);
            }
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode node, params object[] messageArgs) =>
            context.ReportDiagnostic(descriptor, node.GetLocation(), messageArgs);

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken token, params object[] messageArgs) =>
            context.ReportDiagnostic(descriptor, token.GetLocation(), messageArgs);

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object[] messageArgs) =>
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }
}
