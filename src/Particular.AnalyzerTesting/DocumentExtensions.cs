namespace Particular.Analyzers.Tests.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;

    static class DocumentExtensions
    {
        public static async Task<IEnumerable<Diagnostic>> GetCompilerDiagnostics(this Document document, CancellationToken cancellationToken = default) =>
            (await document.GetSemanticModelAsync(cancellationToken))
                .GetDiagnostics(cancellationToken: cancellationToken)
                .Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden)
                .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
                .ThenBy(diagnostic => diagnostic.Id);
    }
}
