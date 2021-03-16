namespace Particular.CodeRules
{
    using Microsoft.CodeAnalysis;

    public static class SuppressionDescriptors
    {
        public static readonly SuppressionDescriptor CancellationTokenParameterUnused = new SuppressionDescriptor(
            SuppressonIds.CancellationTokenParameterUnused,
            "IDE0060",
            "Allow CA2016 to suggest forwarding the CancellationToken parameter to methods that may take one in the future.");
    }
}
