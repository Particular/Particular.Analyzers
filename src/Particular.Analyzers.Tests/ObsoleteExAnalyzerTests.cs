namespace Particular.Analyzers.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

[TestFixture]
public class ObsoleteExAnalyzerTests
{
    [Test]
    public void Should_warn_when_version_below()
    {
        var source =
            @"using System;

[assembly: System.Reflection.AssemblyVersionAttribute(""3.0.0.0"")]

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property,
    AllowMultiple = false)]
internal class ObsoleteExAttribute : Attribute
{
    public string Message { get; set; }
    public string TreatAsErrorFromVersion { get; set; }
    public string RemoveInVersion { get; set; }
    public string ReplacementTypeOrMember { get; set; }
}

[ObsoleteEx(
    Message = ""Custom message."", 
    TreatAsErrorFromVersion = ""2.0"", 
    RemoveInVersion = ""4.0"", 
    ReplacementTypeOrMember = ""NewClass"")]
public partial class Class
{
}";
        var (output, _) = GetGeneratedOutput(source);

        Console.WriteLine(output);
    }

    static (string output, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source,
        bool suppressGeneratedDiagnosticsErrors = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = Compile([syntaxTree], references);

        var generator = new ObsoleteExSourceGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

        // add necessary references for the generated trigger
        // references.Add(MetadataReference.CreateFromFile(typeof(ServiceBusTriggerAttribute).Assembly.Location));
        // references.Add(MetadataReference.CreateFromFile(typeof(FunctionContext).Assembly.Location));
        // references.Add(MetadataReference.CreateFromFile(typeof(ServiceBusReceivedMessage).Assembly.Location));
        // references.Add(MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location));
        Compile(outputCompilation.SyntaxTrees, references);

        if (!suppressGeneratedDiagnosticsErrors)
        {
            Assert.That(generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False,
                "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());
        }

        return (outputCompilation.SyntaxTrees.Last().ToString(), generateDiagnostics);
    }

    static CSharpCompilation Compile(IEnumerable<SyntaxTree> syntaxTrees, IEnumerable<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create("result", syntaxTrees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Verify the code compiled:
        var compilationErrors = compilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.That(compilationErrors, Is.Empty, compilationErrors.FirstOrDefault()?.GetMessage());

        return compilation;
    }
}