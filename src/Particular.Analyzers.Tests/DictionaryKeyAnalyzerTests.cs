namespace Particular.Analyzers.Tests
{
    using System.Collections.Concurrent;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using AnalyzerTesting;
    using NUnit.Framework;

    public class DictionaryKeysAnalyzerTests : AnalyzerTestFixture<DictionaryKeysAnalyzer>
    {
        public DictionaryKeysAnalyzerTests()
        {
            AddMetadataReferenceUsing<ConcurrentDictionary<string, string>>();
            AddMetadataReferenceUsing<ImmutableDictionary<string, string>>();
        }

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

            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }

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

        [Test]
        public Task IgnoreWhenCustomComparerInUse()
        {
            var code = """
                using System.Collections.Generic;

                public class CustomType
                {
                    public int Value { get; set; }
                }
                public class CustomComparer : IEqualityComparer<CustomType>
                {
                    public bool Equals(CustomType x, CustomType y) => x.Value == y.Value;
                    public int GetHashCode(CustomType obj) => obj.Value.GetHashCode();
                    public static readonly CustomComparer Instance = new CustomComparer();
                    public static CustomComparer Create() => new CustomComparer();
                }
                public class Foo
                {
                    public void Bar()
                    {
                        var badSet1 = new [|HashSet<CustomType>|]();
                        [|HashSet<CustomType>|] badSet2 = new();

                        var goodSet1 = new HashSet<CustomType>(new CustomComparer());
                        HashSet<CustomType> goodSet2 = new(new CustomComparer());

                        var goodSet3 = new HashSet<CustomType>(CustomComparer.Instance);
                        HashSet<CustomType> goodSet4 = new(CustomComparer.Instance);

                        // Multiple declarators are rare but technically possible
                        // Note that implicitly typed variables (using var) cannot have multiple declarators like this
                        [|HashSet<CustomType>|] badMult1 = new [|HashSet<CustomType>|](),
                                            badMult2 = new(),
                                            badMult3 = new(CustomComparer.Instance); // This is OK but still have to flag the initial type declaration


                        HashSet<CustomType> goodMult1 = new HashSet<CustomType>(new CustomComparer()),
                                            goodMult2 = new(CustomComparer.Instance);

                        var comparerViaMethod = new HashSet<CustomType>(CustomComparer.Create());
                    }
                }
                """;

            return Assert(code, DiagnosticIds.DictionaryHasUnsupportedKeyType);
        }
    }
}
