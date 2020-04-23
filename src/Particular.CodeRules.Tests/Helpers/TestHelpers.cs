using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Particular.CodeRules.Tests
{
    public static class TestHelpers
    {
        public static bool TryGetCodeAndSpanFromMarkup(string markupCode, out string code, out TextSpan span)
        {
            if (markupCode == null) throw new ArgumentNullException(nameof(markupCode));

            code = null;
            span = default;

            var builder = new StringBuilder();

            var start = markupCode.IndexOf("[|", System.StringComparison.Ordinal);
            if (start < 0)
            {
                return false;
            }

            builder.Append(markupCode.Substring(0, start));

            var end = markupCode.IndexOf("|]", System.StringComparison.Ordinal);
            if (end < 0)
            {
                return false;
            }

            builder.Append(markupCode.Substring(start + 2, end - start - 2));
            builder.Append(markupCode.Substring(end + 2));

            code = builder.ToString();
            span = TextSpan.FromBounds(start, end - 2);

            return true;
        }

        public static bool TryGetDocumentAndSpanFromMarkup(string markupCode, string languageName, out Document document, out TextSpan span)
        {
            return TryGetDocumentAndSpanFromMarkup(markupCode, languageName, null, out document, out span);
        }

        public static bool TryGetDocumentAndSpanFromMarkup(string markupCode, string languageName, ImmutableList<MetadataReference> references, out Document document, out TextSpan span)
        {
            if (!TryGetCodeAndSpanFromMarkup(markupCode, out var code, out span))
            {
                document = null;
                return false;
            }

            document = GetDocument(code, languageName, references);
            return true;
        }

        public static Document GetDocument(string code, string languageName, ImmutableList<MetadataReference> references = null)
        {
            references = references ?? ImmutableList.Create<MetadataReference>(
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.GetLocation()),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.GetLocation()));

#pragma warning disable CA2000 // Dispose objects before losing scope
            return new AdhocWorkspace()
#pragma warning restore CA2000 // Dispose objects before losing scope
                .AddProject("TestProject", languageName)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(references)
                .AddDocument("TestDocument", code);
        }
    }
}