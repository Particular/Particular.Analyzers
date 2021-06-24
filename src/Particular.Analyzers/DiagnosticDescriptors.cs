namespace Particular.Analyzers
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor DroppedTask = new DiagnosticDescriptor(
            id: DiagnosticIds.DroppedTask,
            title: "Tasks returned from expressions should be returned, awaited, or assigned to a variable",
            messageFormat: "Return, await, or assign the task to a variable",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CancellableContextMethodCancellationToken = new DiagnosticDescriptor(
            id: DiagnosticIds.CancellableContextMethodCancellationToken,
            title: "Instance methods on types implementing ICancellableContext should not have a CancellationToken parameter",
            messageFormat: "Remove the CancellationToken parameter",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CancellationTokenNonPrivateRequired = new DiagnosticDescriptor(
            id: DiagnosticIds.CancellationTokenNonPrivateRequired,
            title: "A parameter of type CancellationToken on a non-private delegate or method should be optional",
            messageFormat: "Make the CancellationToken parameter optional",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CancellationTokenPrivateOptional = new DiagnosticDescriptor(
            id: DiagnosticIds.CancellationTokenPrivateOptional,
            title: "A parameter of type CancellationToken on a private delegate or method should be required",
            messageFormat: "Make the CancellationToken parameter required",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DelegateCancellationTokenMisplaced = new DiagnosticDescriptor(
            id: DiagnosticIds.DelegateCancellationTokenMisplaced,
            title: "Delegate CancellationToken parameters should come last",
            messageFormat: "Move the CancellationToken parameter to end of the parameter list",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EmptyCancellationTokenDefaultLiteral = new DiagnosticDescriptor(
            id: DiagnosticIds.EmptyCancellationTokenDefaultLiteral,
            title: "The default literal should not be used as an argument instead of CancellationToken.None",
            messageFormat: "Change the argument to CancellationToken.None",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EmptyCancellationTokenDefaultOperator = new DiagnosticDescriptor(
            id: DiagnosticIds.EmptyCancellationTokenDefaultOperator,
            title: "The default operator should not be used as an argument instead of CancellationToken.None",
            messageFormat: "Change the argument to CancellationToken.None",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterCancellationTokenMisplaced = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodFuncParameterCancellationTokenMisplaced,
            title: "Funcs used as method parameters should have CancellationToken parameter type arguments last",
            messageFormat: "Move the CancellationToken parameter type arguments to end of the parameter type arguments list",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterMixedCancellation = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodFuncParameterMixedCancellation,
            title: "Funcs used as method parameters should not have both CancellationToken parameter type arguments and type arguments implementing ICancellableContext",
            messageFormat: "Remove either the CancellationToken parameter type arguments or the parameter type arguments implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterMultipleCancellableContexts = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodFuncParameterMultipleCancellableContexts,
            title: "Funcs used as method parameters should have at most one parameter type argument implementing ICancellableContext",
            messageFormat: "Remove the duplicate parameter type arguments implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterMultipleCancellationTokens = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodFuncParameterMultipleCancellationTokens,
            title: "Funcs used as method parameters should have at most one CancellationToken parameter type argument",
            messageFormat: "Remove the duplicate CancellationToken parameter type arguments",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterTaskReturnTypeNoCancellation = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation,
            title: "A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext",
            messageFormat: "Add a CancellationToken parameter type argument",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMixedCancellation = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodMixedCancellation,
            title: "Methods should not have both CancellationToken parameters and parameters implementing ICancellableContext",
            messageFormat: "Remove either the CancellationToken parameters or the parameters implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMultipleCancellableContexts = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodMultipleCancellableContexts,
            title: "Methods should have at most one parameter implementing ICancellableContext",
            messageFormat: "Remove the duplicate parameters implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMultipleCancellationTokens = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodMultipleCancellationTokens,
            title: "Methods should have at most one CancellationToken parameter",
            messageFormat: "Remove the duplicate CancellationToken parameters",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodCancellationTokenMisnamed = new DiagnosticDescriptor(
            id: DiagnosticIds.MethodCancellationTokenMisnamed,
            title: "CancellationToken parameters should be named cancellationToken or have names ending with CancellationToken",
            messageFormat: "Rename the CancellationToken parameter to cancellationToken or to end with CancellationToken",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NonPrivateMethodSingleCancellationTokenMisnamed = new DiagnosticDescriptor(
            id: DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed,
            title: "Single, non-private CancellationToken parameters should be named cancellationToken",
            messageFormat: "Rename the CancellationToken parameter to cancellationToken",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TaskReturningMethodNoCancellation = new DiagnosticDescriptor(
            id: DiagnosticIds.TaskReturningMethodNoCancellation,
            title: "A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext",
            messageFormat: "Add a CancellationToken parameter",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ImproperTryCatchSystemException = new DiagnosticDescriptor(
            id: DiagnosticIds.ImproperTryCatchSystemException,
            title: "When catching System.Exception, cancellation needs to be properly accounted for",
            messageFormat: "When a try block involves possible cancellation, catching Exception should be preceded by catching OperationCanceledException, or filtered by exception type and cancellationToken.IsCancellationRequested. See https://go.particular.net/exception-handling-with-cancellation.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ImproperTryCatchOperationCanceled = new DiagnosticDescriptor(
            id: DiagnosticIds.ImproperTryCatchOperationCanceled,
            title: "When catching OperationCanceledException, cancellation needs to be properly accounted for",
            messageFormat: "Catching OperationCanceledException should be filtered by cancellationToken.IsCancellationRequested. See https://go.particular.net/exception-handling-with-cancellation.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleCancellationTokensInATry = new DiagnosticDescriptor(
            id: DiagnosticIds.MultipleCancellationTokensInATry,
            title: "Highlight when a try block passes multiple cancellation tokens",
            messageFormat: "This try block passes more than one CancellationToken (or ICancellableContext) to other methods, which can be confusing. Suppress this message with a #pragma, add a comment explaining the use of the two tokens, and ensure any CancellationToken used in a catch block is the correct one.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DateTimeAssignedToDateTimeOffset = new DiagnosticDescriptor(
            id: DiagnosticIds.DateTimeAssignedToDateTimeOffset,
            title: "Prevent a DateTime from being implicitly assigned to a DateTimeOffset",
            messageFormat: "Do not implicitly assign a DateTime value to a DateTimeOffset variable",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UseUtcNow = new DiagnosticDescriptor(
            id: DiagnosticIds.UseUtcNow,
            title: "Use DateTime.UtcNow or DateTimeOffset.UtcNow",
            messageFormat: "Use {0}.UtcNow instead of {0}.Now",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
