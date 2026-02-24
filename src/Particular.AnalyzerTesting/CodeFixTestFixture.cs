#nullable enable

namespace Particular.AnalyzerTesting;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

public abstract class CodeFixTestFixture<TAnalyzer, TCodeFix> : AnalyzerTestFixture<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    protected async Task Assert(string original, string expected, bool mustCompile = true)
    {
        var originalFiles = MarkupSplitter.SplitMarkup(original).ToDictionary(f => f.Filename);
        var fixedFiles = MarkupSplitter.SplitMarkup(expected).ToDictionary(f => f.Filename);

        NUnit.Framework.Assert.That(originalFiles.Keys, Is.EquivalentTo(fixedFiles.Keys));

        var test = AnalyzerTest.ForAnalyzer<TAnalyzer>("TestProject")
            .WithCodeFix<TCodeFix>()
            .MustCompile(mustCompile);

        ConfigureFixtureTests(test);

        foreach (var file in originalFiles.Values)
        {
            var expectedFile = fixedFiles[file.Filename];
            test.WithCodeFixSource(file.Content, expectedFile.Content, file.Filename);
        }

        await test.AssertCodeFixes();
    }
}