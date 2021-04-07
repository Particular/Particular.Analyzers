namespace Particular.CodeRules.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using Xunit.Abstractions;

    public class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public AnalyzerTestFixture(ITestOutputHelper output) => Output = output;

        AnalyzerTestFixture() { }

        protected static readonly List<string> PrivateModifiers = new List<string> { "", "private" };

        protected static readonly List<string> NonPrivateModifiers = new List<string> { "public", "protected", "internal", "protected internal", "private protected" };

        protected static readonly List<string> InterfacePrivateModifiers = new List<string>
        {
#if NETCOREAPP
            "private",
#endif
        };

        protected static readonly List<string> InterfaceNonPrivateModifiers = new List<string>
        {
            "",
            "public",
            "internal",
#if NETCOREAPP
            "protected",
            "protected internal",
            "private protected",
#endif
        };

        protected ITestOutputHelper Output { get; }

        protected virtual bool Compile => true;

        protected async Task Assert(string markupCode, params string[] expectedDiagnosticIds)
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

            var (code, markupSpans) = Parse(markupCode);

            foreach (var (line, index) in code.Replace("\r\n", "\n").Split('\n')
                .Select((line, index) => (line, index)))
            {
                Output.WriteLine($"{index + 1,3}: {line}");
            }

            var references = ImmutableList.Create<MetadataReference>(
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location));

            var document = new AdhocWorkspace()
                .AddProject("TestProject", LanguageNames.CSharp)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(references)
                .AddDocument("Externaltypes", externalTypes)
                .Project
                .AddDocument("TestDocument", code);

            var analyzer = new TAnalyzer();

            var compilationDiagnostics = new List<Diagnostic>();
            List<Diagnostic> analyzerDiagnostics;

            try
            {
                analyzerDiagnostics =
                    (await document.GetDiagnostics(analyzer, Compile, compilationDiagnostics.Add))
                    .OrderBy(diagnostic => diagnostic.Location.SourceSpan)
                    .ThenBy(diagnostic => diagnostic.Id)
                    .ToList();
            }
            finally
            {
                Output.WriteLine("");
                Output.WriteLine("Compilation diagnostics:");
                foreach (var diagnostic in compilationDiagnostics
                    .Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Hidden)
                    .Select(diagnostic => diagnostic.ToString())
                    .OrderBy(_ => _))
                {
                    Output.WriteLine(diagnostic);
                }
            }

            Output.WriteLine("");
            Output.WriteLine("Analyzer diagnostics:");
            foreach (var diagnostic in analyzerDiagnostics
                .Select(diagnostic => diagnostic.ToString())
                .OrderBy(_ => _))
            {
                Output.WriteLine(diagnostic);
            }

            var expectedIdsAndSpans = expectedDiagnosticIds
                .SelectMany(id => markupSpans.Select(span => (id, span)))
                .OrderBy(item => item.span)
                .ThenBy(item => item.id)
                .ToList();

            var actualIdsAndSpans = analyzerDiagnostics
                .Select(diagnostic => (diagnostic.Id, diagnostic.Location.SourceSpan))
                .ToList();

            Xunit.Assert.Equal(expectedIdsAndSpans, actualIdsAndSpans);
        }

        static (string, List<TextSpan>) Parse(string markupCode)
        {
            if (markupCode == null)
            {
                return (null, new List<TextSpan>());
            }

            var code = new StringBuilder();
            var markupSpans = new List<TextSpan>();

            var remainingCode = markupCode;
            var remainingCodeStart = 0;

            while (remainingCode.Length > 0)
            {
                var beforeAndAfterOpening = remainingCode.Split(new[] { "[|" }, 2, StringSplitOptions.None);

                if (beforeAndAfterOpening.Length == 1)
                {
                    _ = code.Append(beforeAndAfterOpening[0]);
                    break;
                }

                var midAndAfterClosing = beforeAndAfterOpening[1].Split(new[] { "|]" }, 2, StringSplitOptions.None);

                if (midAndAfterClosing.Length == 1)
                {
                    throw new Exception("The markup code does not contain a closing '|]'");
                }

                var markupSpan = new TextSpan(remainingCodeStart + beforeAndAfterOpening[0].Length, midAndAfterClosing[0].Length);

                _ = code.Append(beforeAndAfterOpening[0]).Append(midAndAfterClosing[0]);
                markupSpans.Add(markupSpan);

                remainingCode = midAndAfterClosing[1];
                remainingCodeStart += beforeAndAfterOpening[0].Length + markupSpan.Length;
            }

            return (code.ToString(), markupSpans);
        }
    }
}
