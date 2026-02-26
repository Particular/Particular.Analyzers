#nullable enable

namespace Particular.AnalyzerTesting;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// A test fixture that runs multiple tests on source code using the same Roslyn analyzer.
/// </summary>
public class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Override in a test fixture to change the LanguageVersion used to compile the tests.
    /// </summary>
    public virtual LanguageVersion AnalyzerLanguageVersion { get; } = LanguageVersion.CSharp14;

    /// <summary>
    /// Override in a test fixture to apply configuration to every test in the fixture.
    /// </summary>
    /// <param name="test"></param>
    protected virtual void ConfigureFixtureTests(AnalyzerTest test) { }

    /// <summary>
    /// Assert that the code does not raise any diagnostics.
    /// </summary>
    protected Task Assert(string markupCode) =>
        Assert(markupCode, [], [], true);

    /// <summary>
    /// Assert that the code raises the expected diagnostic in the locations specified by the [|…|] markup.
    /// </summary>
    protected Task Assert(string markupCode, string expectedDiagnosticId) =>
        Assert(markupCode, [expectedDiagnosticId], [], true);

    /// <summary>
    /// Assert that the code raises the expected diagnostic in the locations specified by the [|…|] markup.
    /// </summary>
    protected Task Assert(string markupCode, string[] expectedDiagnosticIds) => Assert(markupCode, expectedDiagnosticIds, [], true);

    /// <summary>
    /// Assert that the code raises the expected diagnostic in the locations specified by the [|…|] markup.
    /// </summary>
    protected Task Assert(string markupCode, string[] expectedDiagnosticIds, string[] ignoreDiagnosticIds)
        => Assert(markupCode, expectedDiagnosticIds, ignoreDiagnosticIds, true);

    /// <summary>
    /// Assert that the code raises the expected diagnostic in the locations specified by the [|…|] markup.
    /// </summary>
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
