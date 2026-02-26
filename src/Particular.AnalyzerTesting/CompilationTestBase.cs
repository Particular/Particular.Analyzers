#nullable enable
namespace Particular.AnalyzerTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// A base class for <see cref="AnalyzerTest" /> and <see cref="SourceGeneratorTest" />.
/// </summary>
public abstract class CompilationTestBase<TSelf> where TSelf : CompilationTestBase<TSelf>
{
    private protected readonly string outputAssemblyName;
    private protected readonly List<DiagnosticAnalyzer> analyzers = [];
    private protected OutputKind buildOutputType = OutputKind.DynamicallyLinkedLibrary;
    private protected bool suppressCompilationErrors;
    private protected readonly Dictionary<string, string> features = [];

    private protected CompilationTestBase(string? outputAssemblyName = null)
    {
        this.outputAssemblyName = outputAssemblyName ?? "TestAssembly";

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                References.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }
    }

    TSelf Self => (TSelf)this;

    /// <summary>
    /// Reference assemblies for the compilation. Can be added fluently using <see cref="AddReferences(MetadataReference[])" /> or <see cref="AddReferences(IEnumerable&lt;MetadataReference&gt;)" />.
    /// </summary>
    public List<MetadataReference> References { get; } = [];

    /// <summary>
    /// The C# version used to compile the test code. Set with <see cref="WithLangVersion" />.
    /// </summary>
    public LanguageVersion LangVersion { get; private set; } = LanguageVersion.CSharp14;

    /// <summary>
    /// Add a Roslyn analyzer to the test.
    /// </summary>
    public TSelf WithAnalyzer<TAnalyzer>() where TAnalyzer : DiagnosticAnalyzer, new()
    {
        analyzers.Add(new TAnalyzer());
        return Self;
    }

    /// <summary>
    /// Set the C# version for the test.
    /// </summary>
    public TSelf WithLangVersion(LanguageVersion langVersion)
    {
        LangVersion = langVersion;
        return Self;
    }

    /// <summary>
    /// Add reference assemblies for the test.
    /// </summary>
    public TSelf AddReferences(params MetadataReference[] references)
        => AddReferences(references.AsEnumerable());

    /// <summary>
    /// Add reference assemblies for the test.
    /// </summary>
    public TSelf AddReferences(IEnumerable<MetadataReference> references)
    {
        References.AddRange(references);
        return Self;
    }

    /// <summary>
    /// Change the build output from ClassLibrary to another <see cref="OutputKind" />.
    /// </summary>
    public TSelf BuildAs(OutputKind outputKind)
    {
        buildOutputType = outputKind;
        return Self;
    }

    /// <summary>
    /// Suppress compilation errors in the test, for analyzers that need to run on code that does not compile,
    /// for example when a code fix exists to update from an obsolete API to a new one.
    /// </summary>
    public TSelf SuppressCompilationErrors()
    {
        suppressCompilationErrors = true;
        return Self;
    }

    /// <summary>
    /// Add an interceptors namespace to the project properties, more easily than using <see cref="WithProperty" /> directly.
    /// </summary>
    public TSelf WithInterceptorNamespace(string interceptorNamespace)
    {
        const string key = "InterceptorsNamespaces";
        if (features.TryGetValue(key, out var value))
        {
            features[key] = $"{value};{interceptorNamespace}";
        }
        else
        {
            features[key] = interceptorNamespace;
        }

        return Self;
    }

    /// <summary>
    /// Add a build property to the compilation.
    /// </summary>
    public TSelf WithProperty(string name, string value)
    {
        features.Add(name, value);
        return Self;
    }
}