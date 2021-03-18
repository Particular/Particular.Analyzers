#pragma warning disable CA2211 // Non-constant fields should not be visible
namespace Particular.CodeRules
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor AwaitOrCaptureTasks = new DiagnosticDescriptor(
            id: DiagnosticIds.AwaitOrCaptureTasks,
            title: "Await or Capture Tasks",
            messageFormat: "Expression creates a Task that is not awaited or assigned to a variable.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A method returning a Task should either be awaited or stored in a variable so that the Task is not dropped.");
    }
}
