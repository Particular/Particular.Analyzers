namespace Particular.Analyzers.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AnalyzerTesting;
    using Helpers;
    using NUnit.Framework;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class TypeNameAnalyzerTests : AnalyzerTestFixture<TypeNameAnalyzer>
    {
        static readonly string @type = "{0} [|{1}|] {{ }}";

        static readonly string @delegate = "delegate void [|{0}|]();";

        static readonly List<string> interfaceKeywords =
        [
           "interface",
        ];

        static readonly List<string> nonInterfaceKeywords =
        [
            "class",
            "enum",
            "struct",
            "record",
        ];

        static readonly List<string> interfaceNames =
        [
            "ISomething",
            "II",
        ];

        static readonly List<string> nonInterfaceNames =
        [
            "Something",
            "International",
            "I",
        ];

        public static Data SadTypesData =>
            nonInterfaceKeywords.SelectMany(keyword => interfaceNames.Select(name => (keyword, name))).ToData();

        public static Data HappyTypesData =>
            interfaceKeywords.SelectMany(keyword => interfaceNames.Select(name => (keyword, name)))
            .Concat(nonInterfaceKeywords.SelectMany(keyword => nonInterfaceNames.Select(name => (keyword, name)))).ToData();

        public static Data SadDelegatesData => interfaceNames.ToData();

        public static Data HappyDelegatesData => nonInterfaceNames.ToData();

        [Test]
        [TestCaseSource(nameof(SadTypesData))]
        public Task SadTypes(string keyword, string name) => Assert(GetTypeCode(@type, keyword, name), DiagnosticIds.NonInterfaceTypePrefixedWithI);

        [Test]
        [TestCaseSource(nameof(HappyTypesData))]
        public Task HappyTypes(string keyword, string name) => Assert(GetTypeCode(@type, keyword, name));

        [Test]
        [TestCaseSource(nameof(SadDelegatesData))]
        public Task SadDelegates(string name) => Assert(GetDelegateCode(@delegate, name), DiagnosticIds.NonInterfaceTypePrefixedWithI);

        [Test]
        [TestCaseSource(nameof(HappyDelegatesData))]
        public Task HappyDelegates(string name) => Assert(GetDelegateCode(@delegate, name));

        static string GetTypeCode(string template, string keyword, string name) => string.Format(template, keyword, name);

        static string GetDelegateCode(string template, string name) => string.Format(template, name);
    }
}
