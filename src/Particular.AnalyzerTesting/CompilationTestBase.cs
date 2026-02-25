#nullable enable
namespace Particular.AnalyzerTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class CompilationTestBase<TSelf> where TSelf : CompilationTestBase<TSelf>
{
    protected readonly string outputAssemblyName;
    protected readonly List<DiagnosticAnalyzer> analyzers = [];
    protected OutputKind buildOutputType = OutputKind.DynamicallyLinkedLibrary;
    protected bool suppressCompilationErrors;
    protected readonly Dictionary<string, string> features = [];

    protected CompilationTestBase(string? outputAssemblyName = null)
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

    public List<MetadataReference> References { get; } = [];
    public LanguageVersion LangVersion { get; set; } = LanguageVersion.CSharp14;

    public TSelf WithAnalyzer<TAnalyzer>() where TAnalyzer : DiagnosticAnalyzer, new()
    {
        analyzers.Add(new TAnalyzer());
        return Self;
    }

    public TSelf WithLangVersion(LanguageVersion langVersion)
    {
        LangVersion = langVersion;
        return Self;
    }

    public TSelf AddReferences(params MetadataReference[] references)
        => AddReferences(references.AsEnumerable());

    public TSelf AddReferences(IEnumerable<MetadataReference> references)
    {
        References.AddRange(references);
        return Self;
    }

    public TSelf BuildAs(OutputKind outputKind)
    {
        buildOutputType = outputKind;
        return Self;
    }

    public TSelf SuppressCompilationErrors()
    {
        suppressCompilationErrors = true;
        return Self;
    }

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

    public TSelf WithProperty(string name, string value)
    {
        features.Add(name, value);
        return Self;
    }
}