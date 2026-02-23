namespace Particular.AnalyzerTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public partial class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    public virtual LanguageVersion AnalyzerLanguageVersion { get; } = LanguageVersion.CSharp14;

    readonly HashSet<Type> typesForMetadataReferences = [
        typeof(object),
        typeof(Enumerable)
    ];

    protected void AddMetadataReferenceUsing<TTypeFromAssembly>() => typesForMetadataReferences.Add(typeof(TTypeFromAssembly));

    protected Task Assert(string markupCode, CancellationToken cancellationToken = default) =>
        Assert(markupCode, [], cancellationToken);

    protected Task Assert(string markupCode, string expectedDiagnosticId, CancellationToken cancellationToken = default) =>
        Assert(markupCode, [expectedDiagnosticId], cancellationToken);

    protected Task Assert(string markupCode, string[] expectedDiagnosticIds, CancellationToken cancellationToken = default)
    {
        var externalTypes =
            @"namespace NServiceBus
{
interface ICancellableContext { }
class CancellableContext : ICancellableContext { }
interface IMessage { }
}";

        markupCode =
            @"#pragma warning disable CS8019
using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
#pragma warning restore CS8019

" +
            markupCode;

        return AnalyzerTest.ForAnalyzer<TAnalyzer>("TestProject")
            .WithLangVersion(AnalyzerLanguageVersion)
            .WithSource(externalTypes, "ExternalTypes.cs")
            .WithSource(markupCode, "Code.cs")
            .ExpectDiagnosticIds(expectedDiagnosticIds)
            .Run(cancellationToken);
    }
}
