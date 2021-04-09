namespace Particular.Analyzers.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    static class DocumentExtensions
    {
        public static async Task<IEnumerable<Diagnostic>> GetDiagnostics(this Document document, DiagnosticAnalyzer analyzer, Action<Diagnostic> onCompilationDiagnostic)
        {
            var compilation = await document.Project.GetCompilationAsync();

            compilation.Compile(onCompilationDiagnostic);

            var exceptions = new List<ExceptionDispatchInfo>();

            var analyzerOptions = new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                (exception, _, __) => exceptions.Add(ExceptionDispatchInfo.Capture(exception)),
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: false);

            var compilationWithAnalyzers = new CompilationWithAnalyzers(
                compilation,
                ImmutableArray.Create(analyzer),
                analyzerOptions);

            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

            foreach (var exception in exceptions)
            {
                exception.Throw();
            }

            return diagnostics;
        }
    }
}
