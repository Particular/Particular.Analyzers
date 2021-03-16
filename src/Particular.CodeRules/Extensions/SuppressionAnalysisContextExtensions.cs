namespace Particular.CodeRules.Extensions
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    static class SuppressionAnalysisContextExtensions
    {
        public static void ReportSuppression(this SuppressionAnalysisContext context, SuppressionDescriptor descriptor, Diagnostic suppressedDiagnostic) =>
            context.ReportSuppression(Suppression.Create(descriptor, suppressedDiagnostic));
    }
}
