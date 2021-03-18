namespace Particular.CodeRules.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using Xunit;

    public abstract class AnalyzerTestFixture<TAnalyzer> : TestFixture where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected async Task NoDiagnostic(string code, string diagnosticId)
        {
            var document = TestHelpers.GetDocument(code, LanguageName);
            var diagnostics = await GetDiagnostics(document);

            Assert.Empty(diagnostics);
        }

        protected async Task HasDiagnostic(string markupCode, string diagnosticId)
        {
            Assert.True(TestHelpers.TryGetDocumentAndSpanFromMarkup(markupCode, LanguageName, out Document document, out TextSpan span), "No markup detected in test code.");

            var diagnostics = await GetDiagnostics(document);

            _ = Assert.Single(diagnostics);

            var diagnostic = diagnostics[0];

            Assert.Equal(diagnosticId, diagnostic.Id);
            Assert.True(diagnostic.Location.IsInSource, "Diagnostic not in test code.");
            Assert.Equal(span, diagnostic.Location.SourceSpan);
        }

        static async Task<ImmutableArray<Diagnostic>> GetDiagnostics(Document document)
        {
            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer());
            var exceptions = new List<ExceptionDispatchInfo>();
            var compilation = await document.Project.GetCompilationAsync(CancellationToken.None);

            var analyzerOptions = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                (exception, analyzer, diagnostic) => exceptions.Add(ExceptionDispatchInfo.Capture(exception)),
                false,
                false,
                false);

            var compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, analyzerOptions);
            var discarded = compilation.GetDiagnostics(CancellationToken.None);

            var tree = await document.GetSyntaxTreeAsync(CancellationToken.None);

            var builder = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (var diagnostic in await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync())
            {
                var location = diagnostic.Location;

                if (location.IsInSource && location.SourceTree == tree)
                {
                    builder.Add(diagnostic);
                }
            }

            // throw exceptions from analyzers to fail the test
            foreach (var exceptionDispatchInfo in exceptions)
            {
                exceptionDispatchInfo.Throw();
            }

            return builder.ToImmutable();
        }
    }
}
