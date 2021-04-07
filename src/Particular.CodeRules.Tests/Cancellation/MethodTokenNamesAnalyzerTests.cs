namespace Particular.CodeRules.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.CodeRules.Cancellation;
    using Particular.CodeRules.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class MethodTokenNamesAnalyzerTests : AnalyzerTestFixture<MethodTokenNamesAnalyzer>
    {
        static readonly string method =
@"class MyClass
{{
    void MyMethod({0}) {{ }}
}}";

        static readonly string constructor =
@"class MyClass
{{
    MyClass({0}) {{ }}
}}";

        static readonly string @delegate =
@"class MyClass
{{
    delegate void MyDelegate({0});
}}";

        static readonly string @override =
@"class MyBase
{{
    protected virtual void MyMethod({0}) {{ }}
}}

class MyClass : MyBase
{{
    protected override void MyMethod({0}) {{ }}
}}";

        static readonly string @explicit =
@"interface IMyInterface
{{
    void MyMethod({0});
}}

class MyClass : IMyInterface
{{
    void IMyInterface.MyMethod({0}) {{ }}
}}";

        static readonly string interfaceMethods =
@"interface IMyType
{{
    void MyMethod({0});
}}";

#if NETCOREAPP
        static readonly string interfaceDefaultMethods =
@"interface IMyType
{{
    void MyMethod({0}) {{ }}
}}";
#endif

        public MethodTokenNamesAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data SadParams = new List<string>
        {
            "CancellationToken [|token|]",
            "object foo, CancellationToken [|token|]",
            "CancellationToken [|foo|]",
            "object foo, CancellationToken [|bar|]",
            "CancellationToken [|foo|], CancellationToken [|bar|]",
            "object foo, CancellationToken [|bar|], CancellationToken [|baz|]",
        }.ToData();

        public static readonly Data HappyParams = new List<string>
        {
            "CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken",
            "CancellationToken fooCancellationToken",
            "object foo, CancellationToken fooCancellationToken",
            "CancellationToken fooCancellationToken, CancellationToken barCancellationToken",
            "object foo, CancellationToken fooCancellationToken, CancellationToken barCancellationToken",
        }.ToData();

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadMethods(string @params) => Assert(GetCode(method, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyMethods(string @params) => Assert(GetCode(method, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadConstructors(string @params) => Assert(GetCode(constructor, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyConstructors(string @params) => Assert(GetCode(constructor, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadOverrides(string @params) => Assert(GetCode(@override, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyOverrides(string @params) => Assert(GetCode(@override, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadExplicits(string @params) => Assert(GetCode(@explicit, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyExplicits(string @params) => Assert(GetCode(@explicit, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadDelegates(string @params) => Assert(GetCode(@delegate, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyDelegates(string @params) => Assert(GetCode(@delegate, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadInterfaceMethods(string @params) => Assert(GetCode(interfaceMethods, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyInterfaceMethods(string @params) => Assert(GetCode(interfaceMethods, @params));

#if NETCOREAPP
        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadInterfaceDefaultMethods(string @params) => Assert(GetCode(interfaceDefaultMethods, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyInterfaceDefaultMethods(string @params) => Assert(GetCode(interfaceDefaultMethods, @params));
#endif

        static string GetCode(string template, string @params) => string.Format(template, @params);
    }
}
