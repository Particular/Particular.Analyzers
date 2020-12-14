namespace Particular.CodeRules
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticIds
    {
        public const string AwaitOrCaptureTasks = "PCR0002";
        public const string MustImplementIHandleMessages = "PCR0042";
        public const string TooManyIHandleMessagesImplementations = "PCR0043";
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

        public static readonly DiagnosticDescriptor MustImplementIHandleMessages = new DiagnosticDescriptor(
            id: DiagnosticIds.MustImplementIHandleMessages,
            title: "Must implement IHandleMessages<T>",
            messageFormat: "Must create a Handle or HandleAsync method for classes implementing IHandleMessages<T>",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A class implementing IHandleMessages<T> must implement a method named Handle or HandleAsync with parameters T message, IMessageHandlerContext context, and optional CancellationToken cancellationToken.");

        public static readonly DiagnosticDescriptor TooManyIHandleMessagesImplementations = new DiagnosticDescriptor(
            id: DiagnosticIds.TooManyIHandleMessagesImplementations,
            title: "At least one implementation",
            messageFormat: "Duplicate Handle/HandleAsync methods for the same message type.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Too many Handle/HandleAsync methods for the same message type.");
    }
}