#nullable enable

namespace Particular.AnalyzerTesting;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    public virtual LanguageVersion AnalyzerLanguageVersion { get; } = LanguageVersion.CSharp14;

    protected virtual void ConfigureFixtureTests(AnalyzerTest test) { }

    protected Task Assert(string markupCode, CancellationToken cancellationToken = default) =>
        Assert(markupCode, [], cancellationToken);

    protected Task Assert(string markupCode, string expectedDiagnosticId, CancellationToken cancellationToken = default) =>
        Assert(markupCode, [expectedDiagnosticId], cancellationToken);

    protected Task Assert(string markupCode, string[] expectedDiagnosticIds, CancellationToken cancellationToken = default)
    {
        var test = AnalyzerTest.ForAnalyzer<TAnalyzer>("TestProject")
            .WithLangVersion(AnalyzerLanguageVersion);

        ConfigureFixtureTests(test);

        foreach (var file in MarkupSplitter.SplitMarkup(markupCode))
        {
            test.WithSource(file.Content, file.Filename);
        }

        return test.AssertDiagnostics(expectedDiagnosticIds);
    }
}
