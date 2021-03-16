namespace Particular.CodeRules
{
    public static class DiagnosticIds
    {
        public const string AwaitOrCaptureTasks = "PCR0002";
        public const string CancellableContextMethodCancellationToken = "PCR0003";
        public const string CancellationTokenNonPrivateRequired = "PCR0004";
        public const string CancellationTokenPrivateOptional = "PCR0005";
        public const string DelegateCancellationTokenMisplaced = "PCR0006";
        public const string EmptyCancellationTokenDefaultLiteral = "PCR0007";
        public const string EmptyCancellationTokenDefaultOperator = "PCR0008";
        public const string EmptyCancellationTokenNone = "PCR0009";
        public const string MethodCancellationTokenMisnamed = "PCR0010";
        public const string MethodFuncParameterCancellationTokenMisplaced = "PCR0011";
        public const string MethodFuncParameterMixedCancellation = "PCR0012";
        public const string MethodFuncParameterMultipleCancellableContexts = "PCR0013";
        public const string MethodFuncParameterMultipleCancellationTokens = "PCR0014";
        public const string MethodFuncParameterTaskReturnTypeNoCancellation = "PCR0015";
        public const string MethodMixedCancellation = "PCR0016";
        public const string MethodMultipleCancellableContexts = "PCR0017";
        public const string MethodMultipleCancellationTokens = "PCR0018";
        public const string TaskReturningMethodNoCancellation = "PCR0019";
    }
}
