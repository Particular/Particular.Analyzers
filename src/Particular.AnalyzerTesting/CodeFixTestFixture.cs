#nullable enable

namespace Particular.AnalyzerTesting;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

/// <summary>
/// A test fixture that runs multiple tests on source code using a Roslyn analyzer and code fix to ensure that the changes
/// made to the source code match the expected results.
/// </summary>
public abstract class CodeFixTestFixture<TAnalyzer, TCodeFix> : AnalyzerTestFixture<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    /// Assert that the code fix applied to the original code creates the expected output.
    /// </summary>
    protected async Task Assert(string original, string expected, bool mustCompile = true)
    {
        var originalFiles = MarkupSplitter.SplitMarkup(original).ToDictionary(f => f.Filename);
        var fixedFiles = MarkupSplitter.SplitMarkup(expected).ToDictionary(f => f.Filename);

        NUnit.Framework.Assert.That(originalFiles.Keys, Is.EquivalentTo(fixedFiles.Keys));

        var test = AnalyzerTest.ForAnalyzer<TAnalyzer>("TestProject")
            .WithCodeFix<TCodeFix>();

        if (!mustCompile)
        {
            test.SuppressCompilationErrors();
        }

        ConfigureFixtureTests(test);

        foreach (var file in originalFiles.Values)
        {
            var expectedFile = fixedFiles[file.Filename];
            test.WithCodeFixSource(file.Content, expectedFile.Content, file.Filename);
        }

        await test.AssertCodeFixes();
    }
}