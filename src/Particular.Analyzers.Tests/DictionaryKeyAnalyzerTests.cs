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
                    DICTIONARY_TYPE justVar;

                    return asType;
                }
            }

            public class BadKey { }
            public struct OkStruct { }
            public interface IOkBecauseEquatable : IEquatable<IOkBecauseEquatable> { }

            public class ClassWithMembers
            {
                public override bool Equals(object obj) => false;
                public override int GetHashCode() => 42;

                // Operators aren't sufficient, but are a "weird" method type that analyzer can't trip over
                public static bool operator ==(ClassWithMembers a, ClassWithMembers b) => false;
                public static bool operator !=(ClassWithMembers a, ClassWithMembers b) => true;
            }

            public class InheritFromDictionary : DICTIONARY_TYPE { }
            """;

        [Theory]
        [InlineData("string")]
        [InlineData("int")]
        [InlineData("OkStruct")]
        [InlineData("ClassWithMembers")]
        [InlineData("IOkBecauseEquatable")]
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

#if !NETFRAMEWORK
        [Fact]
        public Task RecordTypesOk()
        {
            var code = """
                using System.Collections.Generic;

                public class Foo
                {
                    public Dictionary<GoodRecord, int> Field;
                    public Dictionary<GoodRecord, int> Property { get; set; }
                }

                public record class GoodRecord(string A, int B);
                """;

            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }
#endif
    }
}
