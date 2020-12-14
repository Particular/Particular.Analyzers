namespace Particular.CodeRules.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    public static class TestHelpers
    {
        public static bool TryGetCodeAndSpanFromMarkup(string markupCode, out string code, out TextSpan[] spans)
        {
            var spanList = new List<TextSpan>();

            var builder = new StringBuilder();
            var pos = 0;
            var offset = 0;

            while(pos < markupCode.Length)
            {
                var start = markupCode.IndexOf("[|", pos);
                if(start < 0)
                {
                    break;
                }

                builder.Append(markupCode.Substring(pos, start - pos));
                pos = start;

                var end = markupCode.IndexOf("|]", pos);
                if (end < 0 )
                {
                    break;
                }

                spanList.Add(TextSpan.FromBounds(start - offset, end - 2 - offset));
                offset += 4;

                builder.Append(markupCode.Substring(start + 2, end - start - 2));
                pos = end + 2;
            }

            builder.Append(markupCode.Substring(pos));

            code = builder.ToString();
            spans = spanList.ToArray();

            return spans.Length > 0;
        }

        public static bool TryGetDocumentAndSpanFromMarkup(string markupCode, string languageName, out Document document, out TextSpan[] spans)
        {
            return TryGetDocumentAndSpanFromMarkup(markupCode, languageName, null, out document, out spans);
        }

        public static bool TryGetDocumentAndSpanFromMarkup(string markupCode, string languageName, ImmutableList<MetadataReference> references, out Document document, out TextSpan[] spans)
        {
            string code;
            if (!TryGetCodeAndSpanFromMarkup(markupCode, out code, out spans))
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

            return new AdhocWorkspace()
                .AddProject("TestProject", languageName)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(references)
                .AddDocument("TestDocument", code);
        }
    }
}