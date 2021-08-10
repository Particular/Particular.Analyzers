namespace Particular.Analyzers.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class TypeNameAnalyzerTests : AnalyzerTestFixture<TypeNameAnalyzer>
    {
        static readonly string @type = "{0} [|{1}|] {{ }}";

        static readonly string @delegate = "delegate void [|{0}|]();";

        public TypeNameAnalyzerTests(ITestOutputHelper output) : base(output) { }

        static readonly List<string> interfaceKeywords = new List<string>
        {
           "interface",
        };

        static readonly List<string> nonInterfaceKeywords = new List<string>
        {
            "class",
            "enum",
            "struct",
            "record",
        };

        static readonly List<string> interfaceNames = new List<string>
        {
            "ISomething",
            "II",
        };

        static readonly List<string> nonInterfaceNames = new List<string>
        {
            "Something",
            "International",
            "I",
        };

        public static Data SadTypesData =>
            nonInterfaceKeywords.SelectMany(keyword => interfaceNames.Select(name => (keyword, name))).ToData();

        public static Data HappyTypesData =>
            interfaceKeywords.SelectMany(keyword => interfaceNames.Select(name => (keyword, name)))
            .Concat(nonInterfaceKeywords.SelectMany(keyword => nonInterfaceNames.Select(name => (keyword, name)))).ToData();

        public static Data SadDelegatesData => interfaceNames.ToData();

        public static Data HappyDelegatesData => nonInterfaceNames.ToData();

        [Theory]
        [MemberData(nameof(SadTypesData))]
        public Task SadTypes(string keyword, string name) => Assert(GetTypeCode(@type, keyword, name), DiagnosticIds.NonInterfaceTypePrefixedWithI);

        [Theory]
        [MemberData(nameof(HappyTypesData))]
        public Task HappyTypes(string keyword, string name) => Assert(GetTypeCode(@type, keyword, name));

        [Theory]
        [MemberData(nameof(SadDelegatesData))]
        public Task SadDelegates(string name) => Assert(GetDelegateCode(@delegate, name), DiagnosticIds.NonInterfaceTypePrefixedWithI);

        [Theory]
        [MemberData(nameof(HappyDelegatesData))]
        public Task HappyDelegates(string name) => Assert(GetDelegateCode(@delegate, name));

        static string GetTypeCode(string template, string keyword, string name) => string.Format(template, keyword, name);

        static string GetDelegateCode(string template, string name) => string.Format(template, name);
    }
}
