using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class CSharpAnalyzerTestFixture<TAnalyzer> : AnalyzerTestFixture where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override DiagnosticAnalyzer CreateAnalyzer() => new TAnalyzer();
    }

    public abstract class AnalyzerTestFixture : BaseTestFixture
    {
        protected abstract DiagnosticAnalyzer CreateAnalyzer();

        protected Task NoDiagnostic(string code)
        {
            var document = TestHelpers.GetDocument(code, LanguageName);
            return NoDiagnostic(document);
        }

        protected async Task NoDiagnostic(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var diagnostics = await GetDiagnostics(document).ConfigureAwait(false);
            Assert.Empty(diagnostics);
        }

        protected Task HasDiagnostic(string markupCode, string diagnosticId)
        {
            Assert.True(TestHelpers.TryGetDocumentAndSpanFromMarkup(markupCode, LanguageName, out var document, out var span), "No markup detected in test code.");

            return HasDiagnostic(document, span, diagnosticId);
        }

        protected async Task HasDiagnostic(Document document, TextSpan span, string diagnosticId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var diagnostics = await GetDiagnostics(document).ConfigureAwait(false);
            Assert.Single(diagnostics);

            var diagnostic = diagnostics[0];
            Assert.Equal(diagnosticId, diagnostic.Id);
            Assert.True(diagnostic.Location.IsInSource, "Diagnostic not in test code.");
            Assert.Equal(span, diagnostic.Location.SourceSpan);
        }

        private async Task<ImmutableArray<Diagnostic>> GetDiagnostics(Document document)
        {
            var analyzers = ImmutableArray.Create(CreateAnalyzer());
            var exceptions = new List<ExceptionDispatchInfo>();
            var compilation = await document.Project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
            var analyzerOptions = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                (exception, analyzer, diagnostic) => exceptions.Add(ExceptionDispatchInfo.Capture(exception)),
                false,
                false,
                false);
            var compilationWithAnalyzers = new CompilationWithAnalyzers(compilation, analyzers, analyzerOptions);
            var discarded = compilation.GetDiagnostics(CancellationToken.None);

            var tree = await document.GetSyntaxTreeAsync(CancellationToken.None).ConfigureAwait(false);

            var builder = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var diagnostic in await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
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