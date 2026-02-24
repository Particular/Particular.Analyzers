#nullable enable
namespace Particular.AnalyzerTesting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

public partial class AnalyzerTest
{
    readonly string outputAssemblyName;
    readonly List<(string Filename, string MarkupSource)> sources = [];
    readonly List<(string Filename, string Expected)> expectedFixResults = [];
    readonly List<DiagnosticAnalyzer> analyzers = [];
    readonly List<CodeFixProvider> codeFixes = [];
    readonly List<string> commonUsings = [];
    bool mustCompile = true;
    static Action<AnalyzerTest>? configureAllTests;

    public List<MetadataReference> References { get; } = [];
    public LanguageVersion LangVersion { get; set; } = LanguageVersion.CSharp14;

    AnalyzerTest(string? outputAssemblyName = null)
    {
        this.outputAssemblyName = outputAssemblyName ?? "TestAssembly";

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                References.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        configureAllTests?.Invoke(this);
    }

    public static void ConfigureAllAnalyzerTests(Action<AnalyzerTest> configure)
        => configureAllTests = configure;

    public static AnalyzerTest ForAnalyzer<TAnalyzer>([CallerMemberName] string? outputAssemblyName = null)
        where TAnalyzer : DiagnosticAnalyzer, new()
        => new AnalyzerTest(outputAssemblyName).WithAnalyzer<TAnalyzer>();

    public AnalyzerTest WithSource(string source, string? filename = null)
    {
        filename ??= $"CodeFile{sources.Count:00}.cs";
        sources.Add((filename, source));
        return this;
    }

    public AnalyzerTest WithCodeFixSource(string source, string expectedResult, string? filename = null)
    {
        filename ??= $"CodeFile{sources.Count:00}.cs";
        sources.Add((filename, source));
        expectedFixResults.Add((filename, expectedResult));
        return this;
    }

    public AnalyzerTest WithAnalyzer<TAnalyzer>() where TAnalyzer : DiagnosticAnalyzer, new()
    {
        analyzers.Add(new TAnalyzer());
        return this;
    }

    public AnalyzerTest WithCodeFix<TCodeFix>() where TCodeFix : CodeFixProvider, new()
    {
        codeFixes.Add(new TCodeFix());
        return this;
    }

    public AnalyzerTest WithLangVersion(LanguageVersion langVersion)
    {
        LangVersion = langVersion;
        return this;
    }

    public AnalyzerTest AddReferences(params MetadataReference[] references)
        => AddReferences(references.AsEnumerable());

    public AnalyzerTest AddReferences(IEnumerable<MetadataReference> references)
    {
        References.AddRange(references);
        return this;
    }

    public AnalyzerTest WithCommonUsings(params string[] namespaceNames)
    {
        commonUsings.AddRange(namespaceNames);
        return this;
    }

    public AnalyzerTest MustCompile(bool codeMustCompile)
    {
        mustCompile = codeMustCompile;
        return this;
    }

    [GeneratedRegex(@"\r?\n", RegexOptions.Compiled)]
    private static partial Regex NewLineRegex();

    public async Task AssertCodeFixes()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var codeSources = sources.Select(s => CreateFile(s.Filename, s.MarkupSource, parseDiagnosticMarkup: false))
            .ToImmutableArray();
        OutputCode(codeSources);

        var expectedResults = expectedFixResults.Select(s => CreateFile(s.Filename, s.Expected, parseDiagnosticMarkup: false))
            .ToImmutableArray();

        var currentSources = codeSources.ToArray();

        while (true)
        {
            var project = CreateProject(currentSources);
            var compilerDiagnostics = await GetCompilerDiagnostics(project, cancellationToken);

            var compilation = await project.GetCompilationAsync(cancellationToken);
            compilation.Compile(mustCompile);

            var analyzerDiagnostics = await GetAnalyzerDiagnostics(compilation, [], cancellationToken);

            if (analyzerDiagnostics.Length == 0)
            {
                break;
            }

            var actions = await GetCodeFixActions(project, analyzerDiagnostics, cancellationToken);
            if (actions.Length == 0)
            {
                break;
            }
            var actionsByDocumentName = actions.ToLookup(a => a.Document.Name);

            List<SourceFile> updatedSources = [];
            foreach (var projectDocument in project.Documents)
            {
                var document = projectDocument;
                var docActions = actionsByDocumentName[document.Name];
                // TODO: Replace Take(1) with First() and remove loop if I keep this
                foreach (var action in docActions.Take(1))
                {
                    var operations = await action.Action.GetOperationsAsync(cancellationToken);
                    var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
                    document = solution.GetDocument(document!.Id);
                    var tmpSrc = await GetUpdatedCode(document!, cancellationToken);
                    _ = tmpSrc;
                }

                if (document is not null)
                {
                    var updatedSource = await GetUpdatedCode(document, cancellationToken);
                    updatedSources.Add(new(projectDocument.Name, updatedSource, []));
                }
            }

            currentSources = updatedSources.ToArray();

            // var updatedProject = CreateProject(updatedSources);
            // var updatedDiagnostics = await GetCompilerDiagnostics(updatedProject, cancellationToken);
            //
            // if (fixMustCompile)
            // {
            //     Assert.That(updatedDiagnostics, Is.EqualTo(compilerDiagnostics).AsCollection, "Fix introduced new compiler diagnostics.");
            // }
        }

        var updatedSourcesByFilename = currentSources.ToDictionary(s => s.Filename);
        foreach (var expectedSource in expectedResults)
        {
            var updated = updatedSourcesByFilename.GetValueOrDefault(expectedSource.Filename);
            Assert.That(updated, Is.Not.Null, $"No updated code for source filename {expectedSource.Filename}");
            Assert.That(updated!.Source, Is.EqualTo(expectedSource.Source).IgnoreLineEndingFormat);
        }
    }

    async Task<string> GetUpdatedCode(Document document, CancellationToken cancellationToken)
    {
        var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation, cancellationToken: cancellationToken);
        var root = await simplifiedDoc.GetSyntaxRootAsync(cancellationToken);
        root = Formatter.Format(root!, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace, cancellationToken: cancellationToken);
        return root.GetText().ToString();

    }

    async Task<(Document Document, CodeAction Action)[]> GetCodeFixActions(Project project, Diagnostic[] diagnostics, CancellationToken cancellationToken)
    {
        var diagnosticsByFile = diagnostics.ToLookup(d => d.Location.SourceTree!.FilePath);
        var fixesById = codeFixes.SelectMany(fix => fix.FixableDiagnosticIds.Select(id => (id, fix)))
            .ToLookup(f => f.id, f => f.fix);

        var actions = new List<(Document Document, CodeAction Action)>();

        foreach (var document in project.Documents)
        {
            foreach (var diagnostic in diagnosticsByFile[document.Name])
            {
                foreach (var fixProvider in fixesById[diagnostic.Id])
                {
                    var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add((document, action)), cancellationToken);
                    await fixProvider.RegisterCodeFixesAsync(context);
                }
            }
        }

        return [.. actions];
    }

    public Task AssertDiagnostics(params string[] expectedDiagnosticIds) => AssertDiagnostics(expectedDiagnosticIds, []);

    public async Task AssertDiagnostics(string[] expectedDiagnosticIds, string[] ignoreDiagnosticIds)
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;

        var codeSources = sources.Select(s => CreateFile(s.Filename, s.MarkupSource, parseDiagnosticMarkup: true))
            .ToImmutableArray();
        OutputCode(codeSources);

        var project = CreateProject(codeSources);
        _ = await GetCompilerDiagnostics(project, cancellationToken);

        var compilation = await project.GetCompilationAsync(cancellationToken);
        compilation.Compile(mustCompile);

        var analyzerDiagnostics = await GetAnalyzerDiagnostics(compilation, ignoreDiagnosticIds, cancellationToken);

        var expectedDiagnostics = codeSources.SelectMany(src => src.Spans.Select(span => (src.Filename, span)))
            .SelectMany(src => expectedDiagnosticIds.Select(id => new DiagnosticInfo(src.Filename, src.span, id)));

        var actualDiagnostics = analyzerDiagnostics
            .Select(diagnostic => new DiagnosticInfo(diagnostic.Location.SourceTree?.FilePath ?? "<null-file>", diagnostic.Location.SourceSpan, diagnostic.Id));

        Assert.That(actualDiagnostics, Is.EqualTo(expectedDiagnostics));
    }

    Project CreateProject(IEnumerable<SourceFile> codeSources)
    {
        var project = new AdhocWorkspace()
            .AddProject(outputAssemblyName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(References);

        foreach (var source in codeSources)
        {
            project = project.AddDocument(source.Filename, source.Source).Project;
        }

        return project;
    }

    static async Task<Diagnostic[]> GetCompilerDiagnostics(Project project, CancellationToken cancellationToken)
    {
        var compilerDiagnostics = (await Task.WhenAll(project.Documents
                .Select(doc => doc.GetCompilerDiagnostics(cancellationToken))))
            .SelectMany(diagnostics => diagnostics)
            .ToArray();

        OutputCompilerDiagnostics(compilerDiagnostics);
        return compilerDiagnostics;
    }

    async Task<Diagnostic[]> GetAnalyzerDiagnostics(Compilation? compilation, string[] ignoreDiagnosticIds, CancellationToken cancellationToken)
    {
        var analyzerTasks = analyzers
            .Select(analyzer => compilation.GetAnalyzerDiagnostics(analyzer, cancellationToken))
            .ToArray();

        await Task.WhenAll(analyzerTasks);

        var analyzerDiagnostics = analyzerTasks
            .SelectMany(t => t.Result)
            .Where(d => !ignoreDiagnosticIds.Contains(d.Id))
            .ToArray();

        OutputAnalyzerDiagnostics(analyzerDiagnostics);
        return analyzerDiagnostics;
    }

    SourceFile CreateFile(string filename, string sourceCode, bool parseDiagnosticMarkup)
    {
        var code = new StringBuilder(sourceCode.Length + (commonUsings.Count * 20));

        if (commonUsings.Count > 0)
        {
            code.AppendLine("#pragma warning disable CS8019 // Unnecessary using directive");
            foreach (var use in commonUsings)
            {
                code.AppendLine($"using {use};");
            }

            code.AppendLine("#pragma warning restore CS8019");
            code.AppendLine();
        }

        if (!parseDiagnosticMarkup)
        {
            code.Append(sourceCode);
            return new SourceFile(filename, code.ToString(), []);
        }

        var markupSpans = new List<TextSpan>();
        var prefixOffset = code.Length;

        var remainingCode = sourceCode;
        var remainingCodeStart = 0;

        while (remainingCode.Length > 0)
        {
            var beforeAndAfterOpening = remainingCode.Split(["[|"], 2, StringSplitOptions.None);

            if (beforeAndAfterOpening.Length == 1)
            {
                _ = code.Append(beforeAndAfterOpening[0]);
                break;
            }

            var midAndAfterClosing = beforeAndAfterOpening[1].Split(["|]"], 2, StringSplitOptions.None);

            if (midAndAfterClosing.Length == 1)
            {
                throw new Exception("The markup code does not contain a closing '|]'");
            }

            var markupSpan = new TextSpan(prefixOffset + remainingCodeStart + beforeAndAfterOpening[0].Length, midAndAfterClosing[0].Length);

            _ = code.Append(beforeAndAfterOpening[0]).Append(midAndAfterClosing[0]);
            markupSpans.Add(markupSpan);

            remainingCode = midAndAfterClosing[1];
            remainingCodeStart += beforeAndAfterOpening[0].Length + markupSpan.Length;
        }

        return new SourceFile(filename, code.ToString(), [.. markupSpans]);
    }

    static void OutputCode(IEnumerable<SourceFile> codeSources)
    {
        if (!AnalyzerTestFixtureState.VerboseLogging)
        {
            return;
        }

        foreach (var source in codeSources)
        {
            TestContext.Out.WriteLine($"// == {source.Filename} ===============================");
            var lines = NewLineRegex().Split(source.Source)
                .Select((line, index) => (line, index))
                .ToImmutableArray();
            var lineNumberSize = (lines.Length + 1).ToString().Length;
            var format = $$"""{0,{{lineNumberSize}}}: {1}""";

            foreach (var (line, index) in lines)
            {
                TestContext.Out.WriteLine(string.Format(format, index + 1, line));
            }
        }
    }

    static void OutputCompilerDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        if (!AnalyzerTestFixtureState.VerboseLogging)
        {
            return;
        }

        TestContext.Out.WriteLine("Compiler diagnostics:");

        foreach (var diagnostic in diagnostics)
        {
            TestContext.Out.WriteLine($"  {diagnostic}");
        }
    }

    static void OutputAnalyzerDiagnostics(Diagnostic[] analyzerDiagnostics)
    {
        if (!AnalyzerTestFixtureState.VerboseLogging)
        {
            return;
        }

        TestContext.Out.WriteLine("Analyzer diagnostics:");

        foreach (var diagnostic in analyzerDiagnostics)
        {
            TestContext.Out.WriteLine($"  {diagnostic}");
        }
    }

    record SourceFile(string Filename, string Source, TextSpan[] Spans);
    record DiagnosticInfo(string Filename, TextSpan Span, string Id);
}