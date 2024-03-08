namespace Particular.Analyzers
{
    using Microsoft.CodeAnalysis;

    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor DroppedTask = new(
            id: DiagnosticIds.DroppedTask,
            title: "Tasks returned from expressions should be returned, awaited, or assigned to a variable",
            messageFormat: "Return, await, or assign the task to a variable",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CancellableContextMethodCancellationToken = new(
            id: DiagnosticIds.CancellableContextMethodCancellationToken,
            title: "Instance methods on types implementing ICancellableContext should not have a CancellationToken parameter",
            messageFormat: "Remove the CancellationToken parameter",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CancellationTokenNonPrivateRequired = new(
            id: DiagnosticIds.CancellationTokenNonPrivateRequired,
            title: "A parameter of type CancellationToken on a non-private delegate or method should be optional",
            messageFormat: "Make the CancellationToken parameter optional",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CancellationTokenPrivateOptional = new(
            id: DiagnosticIds.CancellationTokenPrivateOptional,
            title: "A parameter of type CancellationToken on a private delegate or method should be required",
            messageFormat: "Make the CancellationToken parameter required",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DelegateCancellationTokenMisplaced = new(
            id: DiagnosticIds.DelegateCancellationTokenMisplaced,
            title: "Delegate CancellationToken parameters should come last",
            messageFormat: "Move the CancellationToken parameter to end of the parameter list",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EmptyCancellationTokenDefaultLiteral = new(
            id: DiagnosticIds.EmptyCancellationTokenDefaultLiteral,
            title: "The default literal should not be used as an argument instead of CancellationToken.None",
            messageFormat: "Change the argument to CancellationToken.None",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EmptyCancellationTokenDefaultOperator = new(
            id: DiagnosticIds.EmptyCancellationTokenDefaultOperator,
            title: "The default operator should not be used as an argument instead of CancellationToken.None",
            messageFormat: "Change the argument to CancellationToken.None",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodCancellationTokenMisnamed = new(
            id: DiagnosticIds.MethodCancellationTokenMisnamed,
            title: "CancellationToken parameters should be named cancellationToken or have names ending with CancellationToken",
            messageFormat: "Rename the CancellationToken parameter to cancellationToken or to end with CancellationToken",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterCancellationTokenMisplaced = new(
            id: DiagnosticIds.MethodFuncParameterCancellationTokenMisplaced,
            title: "Funcs used as method parameters should have CancellationToken parameter type arguments last",
            messageFormat: "Move the CancellationToken parameter type arguments to end of the parameter type arguments list",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterMixedCancellation = new(
            id: DiagnosticIds.MethodFuncParameterMixedCancellation,
            title: "Funcs used as method parameters should not have both CancellationToken parameter type arguments and type arguments implementing ICancellableContext",
            messageFormat: "Remove either the CancellationToken parameter type arguments or the parameter type arguments implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterMultipleCancellableContexts = new(
            id: DiagnosticIds.MethodFuncParameterMultipleCancellableContexts,
            title: "Funcs used as method parameters should have at most one parameter type argument implementing ICancellableContext",
            messageFormat: "Remove the duplicate parameter type arguments implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterMultipleCancellationTokens = new(
            id: DiagnosticIds.MethodFuncParameterMultipleCancellationTokens,
            title: "Funcs used as method parameters should have at most one CancellationToken parameter type argument",
            messageFormat: "Remove the duplicate CancellationToken parameter type arguments",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodFuncParameterTaskReturnTypeNoCancellation = new(
            id: DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation,
            title: "A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext",
            messageFormat: "Add a CancellationToken parameter type argument",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMixedCancellation = new(
            id: DiagnosticIds.MethodMixedCancellation,
            title: "Methods should not have both CancellationToken parameters and parameters implementing ICancellableContext",
            messageFormat: "Remove either the CancellationToken parameters or the parameters implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMultipleCancellableContexts = new(
            id: DiagnosticIds.MethodMultipleCancellableContexts,
            title: "Methods should have at most one parameter implementing ICancellableContext",
            messageFormat: "Remove the duplicate parameters implementing ICancellableContext",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MethodMultipleCancellationTokens = new(
            id: DiagnosticIds.MethodMultipleCancellationTokens,
            title: "Methods should have at most one CancellationToken parameter",
            messageFormat: "Remove the duplicate CancellationToken parameters",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NonPrivateMethodSingleCancellationTokenMisnamed = new(
            id: DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed,
            title: "Single, non-private CancellationToken parameters should be named cancellationToken",
            messageFormat: "Rename the CancellationToken parameter to cancellationToken",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TaskReturningMethodNoCancellation = new(
            id: DiagnosticIds.TaskReturningMethodNoCancellation,
            title: "A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext",
            messageFormat: "Add a CancellationToken parameter",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ImproperTryCatchSystemException = new(
            id: DiagnosticIds.ImproperTryCatchSystemException,
            title: "When catching System.Exception, cancellation needs to be properly accounted for",
            messageFormat: "When a try block involves possible cancellation, catching Exception should be preceded by catching OperationCanceledException, or filtered by exception type and cancellationToken.IsCancellationRequested. See https://go.particular.net/exception-handling-with-cancellation.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ImproperTryCatchOperationCanceled = new(
            id: DiagnosticIds.ImproperTryCatchOperationCanceled,
            title: "When catching OperationCanceledException, cancellation needs to be properly accounted for",
            messageFormat: "Catching OperationCanceledException should be filtered by cancellationToken.IsCancellationRequested. See https://go.particular.net/exception-handling-with-cancellation.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleCancellationTokensInATry = new(
            id: DiagnosticIds.MultipleCancellationTokensInATry,
            title: "Highlight when a try block passes multiple cancellation tokens",
            messageFormat: "This try block passes more than one CancellationToken (or ICancellableContext) to other methods, which can be confusing. Suppress this message with a #pragma, add a comment explaining the use of the two tokens, and ensure any CancellationToken used in a catch block is the correct one.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ImplicitCastFromDateTimeToDateTimeOffset = new(
            id: DiagnosticIds.ImplicitCastFromDateTimeToDateTimeOffset,
            title: "A DateTime should not be implicitly cast to a DateTimeOffset",
            messageFormat: "Do not implicitly cast a DateTime to a DateTimeOffset",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NowUsedInsteadOfUtcNow = new(
            id: DiagnosticIds.NowUsedInsteadOfUtcNow,
            title: "DateTime.UtcNow or DateTimeOffset.UtcNow should be used instead of DateTime.Now and DateTimeOffset.Now, unless the value is being used for displaying the current date-time in a user's local time zone",
            messageFormat: "Use {0}.UtcNow instead of {0}.Now",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NonInterfaceTypePrefixedWithI = new(
            id: DiagnosticIds.NonInterfaceTypePrefixedWithI,
            title: "A non-interface type should not be prefixed with I",
            messageFormat: "Remove the I prefix from {0}",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DictionaryHasUnsupportedKeyType = new(
            id: DiagnosticIds.DictionaryHasUnsupportedKeyType,
            title: "Dictionary keys should implement IEquatable<T>",
            messageFormat: "This {0} uses the type {1} as its key, which is a reference type that does not implement IEquatable<T>, which means that every object added to the collection will be evaluated using reference equality (object.Equals()) to be unique. This is often (but not always) a mistake, especially when used as a cache, because every lookup will be a cache miss.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor StructuredLoggingWithRepeatedToken = new(
            id: DiagnosticIds.StructuredLoggingWithRepeatedToken,
            title: "Structured logging tokens cannot be repeated",
            messageFormat: "A token '{0}' was repeated in a logging format string. While this works with string.Format(), it is known to break when using some structured logging libraries and so must be avoided.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AsyncVoid = new(
            id: DiagnosticIds.AsyncVoid,
            title: "Methods should not be declared async void",
            messageFormat: "An `async void` method is almost always a mistake as nothing can be returned to await. Should only be used for event delegates, in which case this rule should be disabled in that instance.",
            category: "Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
