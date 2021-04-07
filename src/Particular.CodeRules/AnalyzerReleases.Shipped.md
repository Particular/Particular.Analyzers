## Release 0.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
PCR0001 |  Code    |  Warning | UseConfigureAwait

## Release 0.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
PCR0002 |  Code    |  Error   | AwaitOrCaptureTasks

## Release 0.3

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
PCR0001 |  Code    |  Warning | Replaced by CA2007

## Release 1.0

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
PCR0002 |  Code    |  Error   | ID changed to PS0001

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
PS0001  |  Code    |  Error   | AwaitOrCaptureTasks
PS0002  |  Code    |  Warning | CancellableContextMethodCancellationToken
PS0003  |  Code    |  Warning | CancellationTokenNonPrivateRequired
PS0004  |  Code    |  Warning | CancellationTokenPrivateOptional
PS0005  |  Code    |  Warning | DelegateCancellationTokenMisplaced
PS0006  |  Code    |  Warning | EmptyCancellationTokenDefaultLiteral
PS0007  |  Code    |  Warning | EmptyCancellationTokenDefaultOperator
PS0008  |  Code    |  Warning | MethodCancellationTokenMisnamed
PS0009  |  Code    |  Warning | MethodFuncParameterCancellationTokenMisplaced
PS0010  |  Code    |  Warning | MethodFuncParameterMixedCancellation
PS0011  |  Code    |  Warning | MethodFuncParameterMultipleCancellableContexts
PS0012  |  Code    |  Warning | MethodFuncParameterMultipleCancellationTokens
PS0013  |  Code    |  Warning | MethodFuncParameterTaskReturnTypeNoCancellation
PS0014  |  Code    |  Warning | MethodMixedCancellation
PS0015  |  Code    |  Warning | MethodMultipleCancellableContexts
PS0016  |  Code    |  Warning | MethodMultipleCancellationTokens
PS0017  |  Code    |  Warning | NonPrivateMethodSingleCancellationTokenMisnamed
PS0018  |  Code    |  Warning | TaskReturningMethodNoCancellation
