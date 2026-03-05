namespace Particular.Analyzers.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AnalyzerTesting;
    using Helpers;
    using NUnit.Framework;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class DroppedTaskAnalyzerTests : AnalyzerTestFixture<DroppedTaskAnalyzer>
    {
        static readonly string code =
@"class MyClass
{{
    delegate Task MyDelegate();

    Func<Task> myFuncField = () => Task.CompletedTask;
    MyDelegate myDelegateField = () => Task.CompletedTask;

    Func<Task> MyFuncProperty => () => Task.CompletedTask;
    MyDelegate MyDelegateProperty => () => Task.CompletedTask;

    void MyContainingMethod()
    {{
        Func<Task> myFuncLocal = () => Task.CompletedTask;
        MyDelegate myDelegateLocal = () => Task.CompletedTask;

        {0}
    }}
}}";

        public static readonly Data SadMethodData = new List<string>
        {
            "void MyMethod() { [|Task.Delay(0)|]; }",

            "void MyMethod() { [|myFuncField()|]; }",
            "void MyMethod() { [|myFuncLocal()|]; }",
            "void MyMethod(Func<Task> myFuncParameter) { [|myFuncParameter()|]; }",
            "void MyMethod() { [|MyFuncProperty()|]; }",

            "void MyMethod() { [|myDelegateField()|]; }",
            "void MyMethod() { [|myDelegateLocal()|]; }",
            "void MyMethod(MyDelegate myDelegateParameter) { [|myDelegateParameter()|]; }",
            "void MyMethod() { [|MyDelegateProperty()|]; }",

            // one ConfigureAwait should be enough to test that condition
            "void MyMethod() { [|Task.Delay(0).ConfigureAwait(false)|]; }",
        }.ToData();

        public static readonly Data HappyMethodData = new List<string>
        {
            "void MyMethod() { var task = Task.Delay(0); }",
            "async Task MyMethod() => await Task.Delay(0);",
            "void MyMethod() { _ = Task.Delay(0); }",
            "Task MyMethod() => Task.Delay(0);",

            "void MyMethod() { var task = myFuncField(); }",
            "async Task MyMethod() => await myFuncField();",
            "void MyMethod() { _ = myFuncField(); }",
            "Task MyMethod() => myFuncField();",

            "void MyMethod() { var task = myFuncLocal(); }",
            "async Task MyMethod() => await myFuncLocal();",
            "void MyMethod() { _ = myFuncLocal(); }",
            "Task MyMethod() => myFuncLocal();",

            "void MyMethod() { var task = MyFuncProperty(); }",
            "async Task MyMethod() => await MyFuncProperty();",
            "void MyMethod() { _ = MyFuncProperty(); }",
            "Task MyMethod() => MyFuncProperty();",

            "void MyMethod(Func<Task> myFuncParameter) { var task = myFuncParameter(); }",
            "async Task MyMethod(Func<Task> myFuncParameter) => await myFuncParameter();",
            "void MyMethod(Func<Task> myFuncParameter) { _ = myFuncParameter(); }",
            "Task MyMethod(Func<Task> myFuncParameter) => myFuncParameter();",

            "void MyMethod() { var task = myDelegateField(); }",
            "async Task MyMethod() => await myDelegateField();",
            "void MyMethod() { _ = myDelegateField(); }",
            "Task MyMethod() => myDelegateField();",

            "void MyMethod() { var task = myDelegateLocal(); }",
            "async Task MyMethod() => await myDelegateLocal();",
            "void MyMethod() { _ = myDelegateLocal(); }",
            "Task MyMethod() => myDelegateLocal();",

            "void MyMethod(MyDelegate myDelegateParameter) { var task = myDelegateParameter(); }",
            "async Task MyMethod(MyDelegate myDelegateParameter) => await myDelegateParameter();",
            "void MyMethod(MyDelegate myDelegateParameter) { _ = myDelegateParameter(); }",
            "Task MyMethod(MyDelegate myDelegateParameter) => myDelegateParameter();",

            "void MyMethod() { var task = MyDelegateProperty(); }",
            "async Task MyMethod() => await MyDelegateProperty();",
            "void MyMethod() { _ = MyDelegateProperty(); }",
            "Task MyMethod() => MyDelegateProperty();",

            // one ConfigureAwait should be enough to test that condition
            "void MyMethod() { var task = Task.Delay(0).ConfigureAwait(false); }",
        }.ToData();

        [Test]
        [TestCaseSource(nameof(SadMethodData))]
        public Task SadMethods(string method) => Assert(GetCode(method), DiagnosticIds.DroppedTask);

        [Test]
        [TestCaseSource(nameof(HappyMethodData))]
        public Task HappyMethods(string method) => Assert(GetCode(method));

        static string GetCode(string method) => string.Format(code, method);
    }
}
