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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

public partial class AnalyzerTest
{
    readonly string outputAssemblyName;
    readonly List<(string Filename, string MarkupSource)> sources = [];
    readonly List<DiagnosticAnalyzer> analyzers = [];
    readonly List<string> commonUsings = [];
    string[] expectedIds = [];
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
        filename ??= $"Source{sources.Count:00}.cs";
        sources.Add((filename, source));
        return this;
    }

    public AnalyzerTest WithAnalyzer<TAnalyzer>() where TAnalyzer : DiagnosticAnalyzer, new()
    {
        analyzers.Add(new TAnalyzer());
        return this;
    }

    public AnalyzerTest WithLangVersion(LanguageVersion langVersion)
    {
        LangVersion = langVersion;
        return this;
    }

    public AnalyzerTest AddReference(MetadataReference reference)
    {
        References.Add(reference);
        return this;
    }

    public AnalyzerTest WithCommonUsings(params string[] namespaceNames)
    {
        commonUsings.AddRange(namespaceNames);
        return this;
    }

    public AnalyzerTest ExpectDiagnosticIds(params string[] expectedDiagnosticIds)
    {
        expectedIds = expectedDiagnosticIds;
        return this;
    }

    [GeneratedRegex(@"\r?\n", RegexOptions.Compiled)]
    private static partial Regex NewLineRegex();

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var codeSources = sources.Select(s => Parse(s.Filename, s.MarkupSource))
            .ToImmutableArray();

        OutputCode(codeSources);

        var project = new AdhocWorkspace()
            .AddProject(outputAssemblyName, LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(References);

        foreach (var source in codeSources)
        {
            project = project.AddDocument(source.Filename, source.Source).Project;
        }

        var compilerDiagnostics = (await Task.WhenAll(project.Documents
                .Select(doc => doc.GetCompilerDiagnostics(cancellationToken))))
            .SelectMany(diagnostics => diagnostics);

        OutputCompilerDiagnostics(compilerDiagnostics);

        var compilation = await project.GetCompilationAsync(cancellationToken);
        compilation.Compile();

        var analyzerTasks = analyzers
            .Select(analyzer => compilation.GetAnalyzerDiagnostics(analyzer, cancellationToken))
            .ToArray();

        await Task.WhenAll(analyzerTasks);

        var analyzerDiagnostics = analyzerTasks
            .SelectMany(t => t.Result)
            .ToArray();

        OutputAnalyzerDiagnostics(analyzerDiagnostics);

        var expectedDiagnostics = codeSources.SelectMany(src => src.Spans.Select(span => (src.Filename, span)))
            .SelectMany(src => expectedIds.Select(id => new DiagnosticInfo(src.Filename, src.span, id)));

        var actualDiagnostics = analyzerDiagnostics
            .Select(diagnostic => new DiagnosticInfo(diagnostic.Location.SourceTree?.FilePath ?? "<null-file>", diagnostic.Location.SourceSpan, diagnostic.Id));

        Assert.That(actualDiagnostics, Is.EqualTo(expectedDiagnostics));
    }

    SourceFile Parse(string filename, string markupCode)
    {
        var code = new StringBuilder(markupCode.Length + (commonUsings.Count * 20));

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

        var markupSpans = new List<TextSpan>();
        var prefixOffset = code.Length;

        var remainingCode = markupCode;
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

    static void OutputCode(ImmutableArray<SourceFile> codeSources)
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