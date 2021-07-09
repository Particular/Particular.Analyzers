namespace Particular.Analyzers
{
    public static class DiagnosticIds
    {
        public const string DroppedTask = "PS0001";
        public const string CancellableContextMethodCancellationToken = "PS0002";
        public const string CancellationTokenNonPrivateRequired = "PS0003";
        public const string CancellationTokenPrivateOptional = "PS0004";
        public const string DelegateCancellationTokenMisplaced = "PS0005";
        public const string EmptyCancellationTokenDefaultLiteral = "PS0006";
        public const string EmptyCancellationTokenDefaultOperator = "PS0007";
        public const string MethodCancellationTokenMisnamed = "PS0008";
        public const string MethodFuncParameterCancellationTokenMisplaced = "PS0009";
        public const string MethodFuncParameterMixedCancellation = "PS0010";
        public const string MethodFuncParameterMultipleCancellableContexts = "PS0011";
        public const string MethodFuncParameterMultipleCancellationTokens = "PS0012";
        public const string MethodFuncParameterTaskReturnTypeNoCancellation = "PS0013";
        public const string MethodMixedCancellation = "PS0014";
        public const string MethodMultipleCancellableContexts = "PS0015";
        public const string MethodMultipleCancellationTokens = "PS0016";
        public const string NonPrivateMethodSingleCancellationTokenMisnamed = "PS0017";
        public const string TaskReturningMethodNoCancellation = "PS0018";
        public const string ImproperTryCatchSystemException = "PS0019";
        public const string ImproperTryCatchOperationCanceled = "PS0020";
        public const string MultipleCancellationTokensInATry = "PS0021";
        public const string ImplicitCastFromDateTimeToDateTimeOffset = "PS0022";
        public const string NowUsedInsteadOfUtcNow = "PS0023";
    }
}
