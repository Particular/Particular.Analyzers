namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class CatchOperationCanceledShouldFilterForTokenTests : AnalyzerTestFixture<CatchAllShouldOmitOperationCanceledAnalyzer>
    {
        public CatchOperationCanceledShouldFilterForTokenTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data CatchBlocks = new[]
        {
            "[|catch|] (OperationCanceledException) { } catch (Exception) { }",                     // OK in all cases
            "[|catch|] (OperationCanceledException) { } catch { }",                                 // OK in all cases
            "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { } catch (Exception) { }",     // OK in all cases
            "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { } catch { }",                 // OK in all cases
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
        var exOther = new Exception();

        try
        {
            await Test(cancellationToken);
        }
#pragma warning disable CS0168 // Variable is declared but never used
        ##CATCH_BLOCKS##
#pragma warning restore CS0168 // Variable is declared but never used
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}";

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks, "cancellationToken"), DiagnosticIds.CatchOperationCanceledShouldFilterForToken);
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
        var exOther = new Exception();

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

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, "context.Token"), DiagnosticIds.CatchOperationCanceledShouldFilterForToken);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassesCancellableContext(string catchBlocks)
        {
            const string PassesCancellableContextTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();
        var context = new RealCancellableContext();

        try
        {
            await Test(context);
        }
        ##CATCH_BLOCKS##
    }
    Task Test(RealCancellableContext context = null) => Task.CompletedTask;
}
class RealCancellableContext : ICancellableContext { public CancellationToken CancellationToken { get; set; } }
";

            return Assert(GetCode(PassesCancellableContextTemplate, catchBlocks, "context.CancellationToken"), DiagnosticIds.CatchOperationCanceledShouldFilterForToken);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassesNoTokenOrEmptyToken(string catchBlocks)
        {
            const string PassesNoTokenOrEmptyTokenTemplate =
@"class Foo
{
    async Task Bar(CancellationToken context)
    {
        var exOther = new Exception();

        try
        {
            await Test(42, true, 3.1415926);
            await Test(42, true, 3.1415926, default);
            await Test(42, true, 3.1415926, default(CancellationToken));
            await Test(42, true, 3.1415926, CancellationToken.None);

            await Test();
            await Test(default);
            await Test(default(ICancellableContext));
        }
        ##CATCH_BLOCKS##
    }
    Task Test(int i, bool b, double d, CancellationToken cancellationToken = default) => Task.CompletedTask;
    Task Test(ICancellableContext cancellableContext = default) => Task.CompletedTask;
}";

            var noDiagnosticCatchBlocks = catchBlocks.Replace("[|", "").Replace("|]", "");
            return Assert(GetCode(PassesNoTokenOrEmptyTokenTemplate, noDiagnosticCatchBlocks, "CancellationToken.None"), DiagnosticIds.CatchOperationCanceledShouldFilterForToken);
        }

        static string GetCode(string template, string catchBlocks, string tokenIdentifier) => template.Replace("##CATCH_BLOCKS##", catchBlocks.Replace("##TOKEN##", tokenIdentifier));
    }
}
