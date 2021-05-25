namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Linq;
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class CancellationTryCatchAnalyzerTests : AnalyzerTestFixture<CancellationTryCatchAnalyzer>
    {
        public CancellationTryCatchAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data AllCatchBlocks;
        public static readonly Data CatchBlocksWithoutIdentifiers;

        static CancellationTryCatchAnalyzerTests()
        {
            var catchBlocksWithIdentifiers = new[]
            {
                // OK in all cases
                "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { } catch (Exception) { }",
                "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { } catch { }",
                "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { }",
                "catch (Exception ex) when (ex is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch (Exception) { }",
                "catch (Exception ex) when (ex is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch { }",
                "catch (Exception ex) when (!(ex is OperationCanceledException) && ##TOKEN##.IsCancellationRequested) { }",
                // Wrong exception checked
                "[|catch|] (Exception ex) when (exOther is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch (Exception) { }",
                "[|catch|] (Exception ex) when (exOther is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch { }",
                "[|catch|] (Exception ex) when (!(exOther is OperationCanceledException) && ##TOKEN##.IsCancellationRequested) { }",
                "[|catch|] when (!(exOther is OperationCanceledException) && ##TOKEN##.IsCancellationRequested) { }",
            };

            var catchBlocksWithoutIdentifiers = new[]
            {
                // Does not check exception or cancellation token
                "[|catch|] (OperationCanceledException) { } catch (Exception) { }",
                "[|catch|] (OperationCanceledException) { } catch { }",
                "[|catch|] (OperationCanceledException) { }",
                "[|catch|] (Exception) { }",
                "[|catch|] { }",
                // Ignore other types of exceptions
                "catch (InvalidOperationException) { }",
                "catch (InvalidOperationException) { } [|catch|] { }",
                // Filter does not include cancellation token
                "[|catch|] (OperationCanceledException) { } catch (Exception) { }",
                "[|catch|] (OperationCanceledException) { } catch { }",
                "[|catch|] (OperationCanceledException) { }",
                "[|catch|] (Exception ex) when (ex is OperationCanceledException) { } catch (Exception) { }",
                "[|catch|] (Exception ex) when (ex is OperationCanceledException) { } catch { }",
                "[|catch|] (Exception ex) when (!(ex is OperationCanceledException)) { }",
            };

            AllCatchBlocks = catchBlocksWithIdentifiers.Concat(catchBlocksWithoutIdentifiers).ToData();
            CatchBlocksWithoutIdentifiers = catchBlocksWithoutIdentifiers.ToData();
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
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
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}";

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks, "cancellationToken"), DiagnosticIds.ImproperTryCatchHandling);
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
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

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, "context.Token"), DiagnosticIds.ImproperTryCatchHandling);
        }

        [Theory]
        [MemberData(nameof(CatchBlocksWithoutIdentifiers))]
        public Task MethodThatGeneratesToken(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();

        try
        {
            await Test(TokenGenerator());
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
    private CancellationToken TokenGenerator() => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, null), DiagnosticIds.ImproperTryCatchHandling);
        }

        [Theory]
        [MemberData(nameof(CatchBlocksWithoutIdentifiers))]
        public Task FuncThatGeneratesToken(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();

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

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, null), DiagnosticIds.ImproperTryCatchHandling);
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
        public Task ThrowIfCancellationRequested(string catchBlocks)
        {
            const string PassesSimpleTokenTemplate =
@"public class Foo
{
    public async Task Bar(CancellationToken cancellationToken)
    {
        var exOther = new Exception();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}";

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks, "cancellationToken"), DiagnosticIds.ImproperTryCatchHandling);
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
        public Task PassesCancellableContext(string catchBlocks)
        {
            const string PassesCancellableContextTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();
        var context = new SomeContext();

        try
        {
            await Test(context);
        }
#pragma warning disable CS0168 // Variable 'ex' declared but never used
        ##CATCH_BLOCKS##
#pragma warning restore CS0168
    }
    Task Test(SomeContext context = null) => Task.CompletedTask;
}
class SomeContext : ICancellableContext { public CancellationToken CancellationToken { get; set; } }
";

            return Assert(GetCode(PassesCancellableContextTemplate, catchBlocks, "context.CancellationToken"), DiagnosticIds.ImproperTryCatchHandling);
        }

        [Theory]
        [MemberData(nameof(CatchBlocksWithoutIdentifiers))]
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
            return Assert(GetCode(PassesNoTokenOrEmptyTokenTemplate, noDiagnosticCatchBlocks, null), DiagnosticIds.ImproperTryCatchHandling);
        }

        static string GetCode(string template, string catchBlocks, string tokenMarkup) => template.Replace("##CATCH_BLOCKS##", catchBlocks.Replace("##TOKEN##", tokenMarkup));
    }
}
