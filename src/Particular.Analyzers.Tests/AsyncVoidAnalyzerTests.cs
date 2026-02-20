namespace Particular.Analyzers.Tests
{
    using System.Threading.Tasks;
    using AnalyzerTesting;
    using NUnit.Framework;

    public class AsyncVoidAnalyzerTests : AnalyzerTestFixture<AsyncVoidAnalyzer>
    {
        [Test]
        public Task NoAsyncVoid()
        {
            var code = """
                public class Foo
                {
                    async void [|Bad1|]() { await Task.Delay(1); }
                    private async void [|Bad2|]() { await Task.Delay(1); }
                    public async void [|Bad3|]() { await Task.Delay(1); }
                    protected async void [|Bad4|]() { await Task.Delay(1); }
                    static async void [|Bad5|]() { await Task.Delay(1); }
                    static private async void [|Bad6|]() { await Task.Delay(1); }
                    static public async void [|Bad7|]() { await Task.Delay(1); }
                    static protected async void [|Bad8|]() { await Task.Delay(1); }
                    async void [|Bad9|](string a, int b) { await Task.Delay(1); }

                    async Task ReturnsTask1() { await Task.Delay(1); }
                    private async Task ReturnsTask2() { await Task.Delay(1); }
                    public async Task ReturnsTask3() { await Task.Delay(1); }
                    protected async Task ReturnsTask4() { await Task.Delay(1); }
                    static async Task ReturnsTask5() { await Task.Delay(1); }
                    static private async Task ReturnsTask6() { await Task.Delay(1); }
                    static public async Task ReturnsTask7() { await Task.Delay(1); }
                    static protected async Task ReturnsTask8() { await Task.Delay(1); }
                    async Task ReturnsTask9(string a, int b) { await Task.Delay(1); }

                    async Task ReturnsValueTask1() { await Task.Delay(1); }
                    private async Task ReturnsValueTask2() { await Task.Delay(1); }
                    public async Task ReturnsValueTask3() { await Task.Delay(1); }
                    protected async Task ReturnsValueTask4() { await Task.Delay(1); }
                    static async Task ReturnsValueTask5() { await Task.Delay(1); }
                    static private async Task ReturnsValueTask6() { await Task.Delay(1); }
                    static public async Task ReturnsValueTask7() { await Task.Delay(1); }
                    static protected async Task ReturnsValueTask8() { await Task.Delay(1); }
                    async Task ReturnsValueTask9(string a, int b) { await Task.Delay(1); }

                    public async Task RegularMethod()
                    {
                        async void [|Local|]() => await Task.Delay(1);

                        await Task.Delay(1);
                    }
                }
                """;

            return Assert(code, DiagnosticIds.AsyncVoid);
        }
    }
}
