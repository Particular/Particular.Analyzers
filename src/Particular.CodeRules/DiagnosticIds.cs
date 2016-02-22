namespace Particular.CodeRules
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticIds
    {
        public const string UseConfigureAwait = "PCR0001";
    }

    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor UseConfigureAwait = new DiagnosticDescriptor(
            id: DiagnosticIds.UseConfigureAwait,
            title: "Await used without specifying ConfigureAwait",
            messageFormat: "ConfigureAwait should be provided here",
            category: DiagnosticCategories.Code,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }

    internal static class DiagnosticCategories
    {
        public const string Code = "Code";
    }
}