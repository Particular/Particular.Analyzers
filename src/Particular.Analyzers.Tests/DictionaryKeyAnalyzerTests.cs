namespace Particular.Analyzers.Tests
{
    using System.Collections.Concurrent;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Analyzers.Tests.Helpers;

    public class DictionaryKeysAnalyzerTests : AnalyzerTestFixture<DictionaryKeysAnalyzer>
    {
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

        [Test]
        [TestCase("string")]
        [TestCase("int")]
        [TestCase("OkStruct")]
        [TestCase("ClassWithMembers")]
        [TestCase("IOkBecauseEquatable")]
        public Task Good(string keyType)
        {
            var dictionaryType = $"Dictionary<{keyType}, int>";
            var code = template.Replace("DICTIONARY_TYPE", dictionaryType);
            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }

        [Test]
        [TestCase("BadKey")]
        public Task Bad(string keyType)
        {
            var dictionaryType = $"Dictionary<{keyType}, int>";
            var code = template.Replace("DICTIONARY_TYPE[]", $"[|{dictionaryType}[]|]")
                .Replace("DICTIONARY_TYPE", $"[|{dictionaryType}|]");
            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }

        [Test]
        [TestCase("IDictionary<BadKey, int>")]
        [TestCase("Dictionary<BadKey, int>")]
        [TestCase("ConcurrentDictionary<BadKey, int>")]
        [TestCase("HashSet<BadKey>")]
        [TestCase("ISet<BadKey>")]
        [TestCase("ImmutableHashSet<BadKey>")]
        [TestCase("ImmutableDictionary<BadKey, int>")]
        public Task CheckTypes(string type)
        {
            var code = $$"""
                using System.Collections.Generic;
                using System.Collections.Concurrent;
                using System.Collections.Immutable;

                public class Foo
                {
                    public [|{{type}}|] Field;
                    public [|{{type}}|] Property { get; set; }
                }

                public class BadKey { }
                """;

            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType, config =>
            {
                config
#if NETFRAMEWORK
                    .AddMetadataReferenceUsing<System.Collections.Generic.ISet<string>>()
#endif
                    .AddMetadataReferenceUsing<ConcurrentDictionary<string, string>>()
                    .AddMetadataReferenceUsing<ImmutableDictionary<string, string>>();
            });
        }

#if NET
        [Test]
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

        [Test]
        public Task IgnoreGenericParameters()
        {
            var code = """
                using System.Collections.Generic;

                public static class Extensions
                {
                    public static void GoodExtensionMethod<T>(this HashSet<T> set)
                    {
                        // User will be warned on the type they are trying to use the method on
                    }
                }
                """;

            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }
    }
}
