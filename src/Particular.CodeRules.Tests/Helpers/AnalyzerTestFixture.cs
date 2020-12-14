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

    public class CSharpAnalyzerTestFixture<TAnalyzer> : AnalyzerTestFixture where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override DiagnosticAnalyzer CreateAnalyzer() => new TAnalyzer();
    }

    public abstract class AnalyzerTestFixture : BaseTestFixture
    {
        protected abstract DiagnosticAnalyzer CreateAnalyzer();

        protected Task NoDiagnostic(string code, string diagnosticId)
        {
            var document = TestHelpers.GetDocument(code, this.LanguageName);
            return this.NoDiagnostic(document, diagnosticId);
        }

        protected async Task NoDiagnostic(Document document, string diagnosticId)
        {
            var diagnostics = await this.GetDiagnostics(document);
            Assert.Empty(diagnostics);
        }

        protected Task HasDiagnostic(string markupCode, string diagnosticId)
        {
            Document document;
            TextSpan[] spans;
            Assert.True(TestHelpers.TryGetDocumentAndSpanFromMarkup(markupCode, this.LanguageName, out document, out spans), "No markup detected in test code.");

            return this.HasDiagnostic(document, spans, diagnosticId);
        }

        protected async Task HasDiagnostic(Document document, TextSpan[] spans, string diagnosticId)
        {
            var diagnostics = await this.GetDiagnostics(document);

            if (diagnostics.Length == spans.Length)
            {
                for (int i = 0; i < spans.Length; i++)
                {
                    var diagnostic = diagnostics[i];
                    Assert.Equal(diagnosticId, diagnostic.Id);
                    Assert.True(diagnostic.Location.IsInSource, "Diagnostic not in test code.");
                    Assert.Equal(spans[i], diagnostic.Location.SourceSpan);
                }
            }
            else if (spans.Length > 1 && diagnostics.Length == 1)
            {
                var diagnostic = diagnostics[0];
                Assert.Equal(diagnosticId, diagnostic.Id);
                Assert.True(diagnostic.Location.IsInSource, "Diagnostic not in test code.");
                Assert.Equal(spans[0], diagnostic.Location.SourceSpan);
                for (int i = 1; i < spans.Length; i++)
                {
                    Assert.Equal(spans[i], diagnostic.AdditionalLocations[i - 1].SourceSpan);
                }
            }
            else
            {
                Assert.True(false, "Test must result in one diagnostic per span, or one diagnostic with AdditionalLocations.");
            }

        }

        private async Task<ImmutableArray<Diagnostic>> GetDiagnostics(Document document)
        {
            var analyzers = ImmutableArray.Create(this.CreateAnalyzer());
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