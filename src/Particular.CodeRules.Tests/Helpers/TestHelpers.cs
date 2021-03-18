namespace Particular.CodeRules.Tests
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    public static class TestHelpers
    {
        static bool TryGetCodeAndSpanFromMarkup(string markupCode, out string code, out TextSpan span)
        {
            code = null;
            span = default;

            var builder = new StringBuilder();

            var start = markupCode.IndexOf("[|");
            if (start < 0)
            {
                return false;
            }

            _ = builder.Append(markupCode.Substring(0, start));

            var end = markupCode.IndexOf("|]");
            if (end < 0)
            {
                return false;
            }

            _ = builder
                .Append(markupCode.Substring(start + 2, end - start - 2));
            _ = builder.Append(markupCode.Substring(end + 2));

            code = builder.ToString();
            span = TextSpan.FromBounds(start, end - 2);

            return true;
        }

        public static bool TryGetDocumentAndSpanFromMarkup(string markupCode, string languageName, out Document document, out TextSpan span)
        {
            if (!TryGetCodeAndSpanFromMarkup(markupCode, out string code, out span))
            {
                document = null;
                return false;
            }

            document = GetDocument(code, languageName);
            return true;
        }

        public static Document GetDocument(string code, string languageName)
        {
            var references = ImmutableList.Create<MetadataReference>(
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location));

            return new AdhocWorkspace()
                .AddProject("TestProject", languageName)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(references)
                .AddDocument("TestDocument", code);
        }
    }
}