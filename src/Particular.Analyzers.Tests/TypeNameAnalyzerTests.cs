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

        public TypeNameAnalyzerTests(ITestOutputHelper output) : base(output) { }

        static readonly List<string> interfaceKeywords = new List<string>
        {
           "interface"
        };

        static readonly List<string> nonInterfaceKeywords = new List<string>
        {
            "class",
            "enum",
            "struct",
            "record"
        };

        static readonly List<string> interfaceNames = new List<string>
        {
            "ISomething",
            "II"
        };

        static readonly List<string> nonInterfaceNames = new List<string>
        {
            "Something",
            "International",
            "I"
        };

        public static Data SadData =>
            nonInterfaceKeywords.SelectMany(keyword => interfaceNames.Select(name => (keyword, name))).ToData();

        public static Data HappyData =>
            interfaceKeywords.SelectMany(keyword => interfaceNames.Select(name => (keyword, name)))
            .Concat(nonInterfaceKeywords.SelectMany(keyword => nonInterfaceNames.Select(name => (keyword, name)))).ToData();

        [Theory]
        [MemberData(nameof(SadData))]
        public Task SadTypes(string keyword, string name) => Assert(GetCode(@type, keyword, name), DiagnosticIds.NonInterfaceTypePrefixedWithI);

        [Theory]
        [MemberData(nameof(HappyData))]
        public Task HappyTypes(string keyword, string name) => Assert(GetCode(@type, keyword, name));

        static string GetCode(string template, string keyword, string name) => string.Format(template, keyword, name);
    }
}
