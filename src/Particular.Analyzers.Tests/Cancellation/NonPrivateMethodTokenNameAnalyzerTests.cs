namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class NonPrivateMethodTokenNameAnalyzerTests : AnalyzerTestFixture<NonPrivateMethodTokenNameAnalyzer>
    {
        static readonly string method =
@"class MyClass
{{
    {0} void MyMethod({1}) {{ }}
}}";

        static readonly string constructor =
@"class MyClass
{{
    {0} MyClass({1}) {{ }}
}}";

        static readonly string @delegate =
@"class MyClass
{{
    {0} delegate void MyDelegate({1});
}}";

        static readonly string @override =
@"class MyBase
{{
    {0} virtual void MyMethod({1}) {{ }}
}}

class MyClass : MyBase
{{
    {0} override void MyMethod({1}) {{ }}
}}";

        static readonly string @explicit =
@"interface IMyInterface
{{
    {0} void MyMethod({1});
}}

class MyClass : IMyInterface
{{
    void IMyInterface.MyMethod({1}) {{ }}
}}";

        static readonly string interfaceMethods =
@"interface IMyType
{{
    {0} void MyMethod({1});
}}";

#if NET
        static readonly string interfaceDefaultMethods =
@"interface IMyType
{{
    {0} void MyMethod({1}) {{ }}
}}";
#endif

        static readonly List<string> sadParams =
        [
            "CancellationToken [|token|]",
            "object foo, CancellationToken [|token|]",
        ];

        static readonly List<string> happyParams =
        [
            "CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken",
            "CancellationToken token1, CancellationToken token2",
            "object foo, CancellationToken token1, CancellationToken token2",
        ];

        public static Data SadData =>
            NonPrivateModifiers.SelectMany(modifiers => sadParams.Select(param => (modifiers, param))).ToData();

        public static Data HappyData =>
            PrivateModifiers.SelectMany(modifiers => sadParams.Concat(happyParams).Select(param => (modifiers, param)))
            .Concat(NonPrivateModifiers.SelectMany(modifiers => happyParams.Select(param => (modifiers, param)))).ToData();

        public static Data HappyOverridesData =>
            NonPrivateModifiers.SelectMany(modifiers => happyParams.Select(param => (modifiers, param))).ToData();

        public static Data SadInterfaceData =>
            InterfaceNonPrivateModifiers.SelectMany(modifiers => sadParams.Select(param => (modifiers, param))).ToData();

        public static Data HappyInterfaceMethodData =>
            InterfaceNonPrivateModifiers.SelectMany(modifiers => happyParams.Select(param => (modifiers, param))).ToData();

        public static Data HappyInterfaceDefaultMethodData =>
            InterfacePrivateModifiers.SelectMany(modifiers => sadParams.Concat(happyParams).Select(param => (modifiers, param)))
            .Concat(InterfaceNonPrivateModifiers.SelectMany(modifiers => happyParams.Select(param => (modifiers, param)))).ToData();

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadMethods(string modifiers, string @params) => Assert(GetCode(method, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyMethods(string modifiers, string @params) => Assert(GetCode(method, modifiers, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadConstructors(string modifiers, string @params) => Assert(GetCode(constructor, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyConstructors(string modifiers, string @params) => Assert(GetCode(constructor, modifiers, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadOverrides(string modifiers, string @params) => Assert(GetCode(@override, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyOverridesData))]
        public Task HappyOverrides(string modifiers, string @params) => Assert(GetCode(@override, modifiers, @params));

        [Test]
        [TestCaseSource(nameof(SadInterfaceData))]
        public Task SadExplicits(string modifiers, string @params) => Assert(GetCode(@explicit, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyInterfaceMethodData))]
        public Task HappyExplicits(string modifiers, string @params) => Assert(GetCode(@explicit, modifiers, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadDelegates(string modifiers, string @params) => Assert(GetCode(@delegate, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyDelegates(string modifiers, string @params) => Assert(GetCode(@delegate, modifiers, @params));

        [Test]
        [TestCaseSource(nameof(SadInterfaceData))]
        public Task SadInterfaceMethods(string modifiers, string @params) => Assert(GetCode(interfaceMethods, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyInterfaceMethodData))]
        public Task HappyInterfaceMethods(string modifiers, string @params) => Assert(GetCode(interfaceMethods, modifiers, @params));

#if NET
        [Test]
        [TestCaseSource(nameof(SadInterfaceData))]
        public Task SadInterfaceDefaultMethods(string modifiers, string @params) => Assert(GetCode(interfaceDefaultMethods, modifiers, @params), DiagnosticIds.NonPrivateMethodSingleCancellationTokenMisnamed);

        [Test]
        [TestCaseSource(nameof(HappyInterfaceDefaultMethodData))]
        public Task HappyInterfaceDefaultMethods(string modifiers, string @params) => Assert(GetCode(interfaceDefaultMethods, modifiers, @params));
#endif

        static string GetCode(string template, string modifiers, string @params) => string.Format(template, modifiers, @params);
    }
}
