namespace Particular.CodeRules
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticIds
    {
        public const string AwaitOrCaptureTasks = "PCR0002";
        public const string AtLeastOneImplementation = "PCR0042";
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

        public static readonly DiagnosticDescriptor AtLeastOneImplementation = new DiagnosticDescriptor(
            id: DiagnosticIds.AtLeastOneImplementation,
            title: "At least one implementation",
            messageFormat: "One of the interface method needs to be implemented.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A class implementing IHandleMessages<T> must implement one of the interface methods.");
    }
}