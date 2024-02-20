namespace Particular.Analyzers.Tests
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class DictionaryKeysAnalyzerTests : AnalyzerTestFixture<DictionaryKeysAnalyzer>
    {
        public DictionaryKeysAnalyzerTests(ITestOutputHelper output) : base(output) { }

        static readonly string template = """
            using System.Collections.Generic;

            public class Foo
            {
                public DICTIONARY_TYPE Field;
                public DICTIONARY_TYPE Property { get; set; }
                public DICTIONARY_TYPE[] FieldArray;
                public DICTIONARY_TYPE[] PropertyArray { get; set; }

                public DICTIONARY_TYPE ParamAndReturn(DICTIONARY_TYPE source)
                {
                    DICTIONARY_TYPE both = new DICTIONARY_TYPE();
                    var asVar = new DICTIONARY_TYPE();
                    DICTIONARY_TYPE asType = new();

                    return asType;
                }
            }

            public class BadKey { }

            public class InheritFromDictionary : DICTIONARY_TYPE { }
            """;

        [Theory]
        [InlineData("string")]
        public Task Good(string keyType)
        {
            var dictionaryType = $"Dictionary<{keyType}, int>";
            var code = template.Replace("DICTIONARY_TYPE", dictionaryType);
            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }

        [Theory]
        [InlineData("BadKey")]
        public Task Bad(string keyType)
        {
            var dictionaryType = $"Dictionary<{keyType}, int>";
            var code = template.Replace("DICTIONARY_TYPE[]", $"[|{dictionaryType}[]|]")
                .Replace("DICTIONARY_TYPE", $"[|{dictionaryType}|]");
            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }
    }
}
