namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class CatchAllShouldOmitOperationCanceledAnalyzerTests : AnalyzerTestFixture<CatchAllShouldOmitOperationCanceledAnalyzer>
    {
        public CatchAllShouldOmitOperationCanceledAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data CatchBlocks = new[]
        {
            "[|catch|] (Exception) { }",                                            // Triggers diagnostic when passing CancellationToken in try
            "[|catch|] { }",                                                        // Triggers diagnostic when passing CancellationToken in try
            "catch (InvalidOperationException) { } [|catch|] (Exception) { }",      // Triggers diagnostic when passing CancellationToken in try
            "catch (InvalidOperationException) { } [|catch|] { }",                  // Triggers diagnostic when passing CancellationToken in try
            "catch (OperationCanceledException) { } catch (Exception) { }",         // OK in all cases
            "catch (OperationCanceledException) { } catch { }",                     // OK in all cases
            "catch (Exception ex) when (!(ex is OperationCanceledException)) { }",  // OK in all cases
            "[|catch|] (Exception ex) when (ex is OperationCanceledException) { }", // Did it wrong, not correct
        }.ToData();

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassingSimpleToken(string catchBlocks)
        {
            const string PassesSimpleTokenTemplate =
@"public class Foo
{
    public async Task Bar(CancellationToken cancellationToken)
    {
        try
        {
            await Test(cancellationToken);
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}";

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassingTokenProperty(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar(SomeContext context)
    {
        try
        {
            await Test(context.Token);
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}
public class SomeContext
{
    public CancellationToken Token => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task MethodThatGeneratesToken(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        try
        {
            await Test(TokenGenerator());
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
    private CancellationToken TokenGenerator() => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task FuncThatGeneratesToken(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        try
        {
            Func<CancellationToken> func = () => CancellationToken.None;
            await Test(func());
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
    private CancellationToken TokenGenerator() => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task NotPassingToken(string catchBlocks)
        {
            const string DoesNotPassTokenTemplate =
@"public class Foo
{
    public async Task Bar(CancellationToken context)
    {
        try
        {
            await Test(42, true, 3.1415926);
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(int i, bool b, double d, CancellationToken cancellationToken = default) => Task.CompletedTask;
}";

            var noDiagnosticCatchBlocks = catchBlocks.Replace("[|", "").Replace("|]", "");
            return Assert(GetCode(DoesNotPassTokenTemplate, noDiagnosticCatchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }


        static string GetCode(string template, string catchBlocks) => template.Replace("##CATCH_BLOCKS##", catchBlocks);
    }
}
