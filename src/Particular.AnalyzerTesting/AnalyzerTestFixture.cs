#nullable enable

namespace Particular.AnalyzerTesting;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    public virtual LanguageVersion AnalyzerLanguageVersion { get; } = LanguageVersion.CSharp14;

    protected virtual void ConfigureFixtureTests(AnalyzerTest test) { }

    protected Task Assert(string markupCode) =>
        Assert(markupCode, [], [], true);

    protected Task Assert(string markupCode, string expectedDiagnosticId) =>
        Assert(markupCode, [expectedDiagnosticId], [], true);

    protected Task Assert(string markupCode, string[] expectedDiagnosticIds) => Assert(markupCode, expectedDiagnosticIds, [], true);

    protected Task Assert(string markupCode, string[] expectedDiagnosticIds, string[] ignoreDiagnosticIds)
        => Assert(markupCode, expectedDiagnosticIds, ignoreDiagnosticIds, true);

    protected Task Assert(string markupCode, string[] expectedDiagnosticIds, string[] ignoreDiagnosticIds, bool mustCompile)
    {
        var test = AnalyzerTest.ForAnalyzer<TAnalyzer>("TestProject")
            .WithLangVersion(AnalyzerLanguageVersion);

        if (!mustCompile)
        {
            test.SuppressCompilationErrors();
        }

        ConfigureFixtureTests(test);

        foreach (var file in MarkupSplitter.SplitMarkup(markupCode))
        {
            test.WithSource(file.Content, file.Filename);
        }

        return test.AssertDiagnostics(expectedDiagnosticIds, ignoreDiagnosticIds);
    }
}
