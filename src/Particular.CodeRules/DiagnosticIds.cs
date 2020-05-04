namespace Particular.CodeRules
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticIds
    {
        public const string AwaitOrCaptureTasks = "PCR0002";
    }

    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor AwaitOrCaptureTasks = new DiagnosticDescriptor(
            id: DiagnosticIds.AwaitOrCaptureTasks,
            title: "Await or Capture Tasks",
            messageFormat: "Expression creates a Task that is not awaited or assigned to a variable.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A method returning a Task should either be awaited or stored in a variable so that the Task is not dropped.");
    }

    internal static class DiagnosticCategories
    {
        public const string Code = "Code";
    }
}