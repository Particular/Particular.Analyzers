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
            var oce = DiagnosticIds.ImproperTryCatchOperationCanceled;
            var ex = DiagnosticIds.ImproperTryCatchSystemException;

            var catchBlocksWithIdentifiers = new (string Diagnostic, string Code)[]
            {
                // OK in all cases
                (oce, "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { } catch (Exception) { }"),
                (oce, "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { } catch { }"),
                (oce, "catch (OperationCanceledException) when (##TOKEN##.IsCancellationRequested) { }"),
                (ex, "catch (Exception ex) when (ex is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch (Exception) { }"),
                (ex, "catch (Exception ex) when (ex is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch { }"),
                (ex, "catch (Exception ex) when (!(ex is OperationCanceledException) && ##TOKEN##.IsCancellationRequested) { }"),
                // Incorrect filter on OCE
                (oce, "[|catch|] (OperationCanceledException) when (##TOKEN##.CanBeCanceled) { } catch (Exception) { }"),
                (oce, "[|catch|] (OperationCanceledException) when (##TOKEN##.CanBeCanceled) { } catch { }"),
                (oce, "[|catch|] (OperationCanceledException) when (##TOKEN##.CanBeCanceled) { }"),
                // Wrong exception checked
                (ex, "[|catch|] (Exception ex) when (exOther is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch (Exception) { }"),
                (ex, "[|catch|] (Exception ex) when (exOther is OperationCanceledException && ##TOKEN##.IsCancellationRequested) { } catch { }"),
                (ex, "[|catch|] (Exception ex) when (!(exOther is OperationCanceledException) && ##TOKEN##.IsCancellationRequested) { }"),
                (ex, "[|catch|] when (!(exOther is OperationCanceledException) && ##TOKEN##.IsCancellationRequested) { }"),
            };

            var catchBlocksWithoutIdentifiers = new (string Diagnostic, string Code)[]
            {
                // Does not check exception type or cancellation token
                (oce, "[|catch|] (OperationCanceledException) when (exOther != null) { } catch (Exception) { }"),
                (oce, "[|catch|] (OperationCanceledException) { } catch (Exception) { }"),
                (oce, "[|catch|] (OperationCanceledException) { } catch { }"),
                (oce, "[|catch|] (OperationCanceledException) { }"),
                (ex, "[|catch|] (Exception) { }"),
                (ex, "[|catch|] { }"),
                // Ignore other types of exceptions
                (ex, "catch (InvalidOperationException) { }"),
                (ex, "catch (InvalidOperationException) { } [|catch|] { }"),
                // Filter does not include cancellation token
                (oce, "[|catch|] (OperationCanceledException) { } catch (Exception) { }"),
                (oce, "[|catch|] (OperationCanceledException) { } catch { }"),
                (oce, "[|catch|] (OperationCanceledException) { }"),
                (ex, "[|catch|] (Exception ex) when (ex is OperationCanceledException) { } catch (Exception) { }"),
                (ex, "[|catch|] (Exception ex) when (ex is OperationCanceledException) { } catch { }"),
                (ex, "[|catch|] (Exception ex) when (!(ex is OperationCanceledException)) { }"),
            };

            AllCatchBlocks = catchBlocksWithIdentifiers.Concat(catchBlocksWithoutIdentifiers).ToData();
            CatchBlocksWithoutIdentifiers = catchBlocksWithoutIdentifiers.ToData();
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
        public Task PassingSimpleToken(string expectedDiagnostic, string catchBlocks)
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

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks, "cancellationToken"), expectedDiagnostic);
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
        public Task PassingTokenProperty(string expectedDiagnostic, string catchBlocks)
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

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, "context.Token"), expectedDiagnostic);
        }

        [Theory]
        [MemberData(nameof(CatchBlocksWithoutIdentifiers))]
        public Task MethodThatGeneratesToken(string expectedDiagnostic, string catchBlocks)
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

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, null), expectedDiagnostic);
        }

        [Theory]
        [MemberData(nameof(CatchBlocksWithoutIdentifiers))]
        public Task FuncThatGeneratesToken(string expectedDiagnostic, string catchBlocks)
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

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks, null), expectedDiagnostic);
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
        public Task ThrowIfCancellationRequested(string expectedDiagnostic, string catchBlocks)
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

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks, "cancellationToken"), expectedDiagnostic);
        }

        [Theory]
        [MemberData(nameof(AllCatchBlocks))]
        public Task PassesCancellableContext(string expectedDiagnostic, string catchBlocks)
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

            return Assert(GetCode(PassesCancellableContextTemplate, catchBlocks, "context.CancellationToken"), expectedDiagnostic);
        }

        [Theory]
        [MemberData(nameof(CatchBlocksWithoutIdentifiers))]
        public Task PassesNoTokenOrEmptyToken(string expectedDiagnostic, string catchBlocks)
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
            return Assert(GetCode(PassesNoTokenOrEmptyTokenTemplate, noDiagnosticCatchBlocks, null), expectedDiagnostic);
        }

        static string GetCode(string template, string catchBlocks, string tokenMarkup) => template.Replace("##CATCH_BLOCKS##", catchBlocks.Replace("##TOKEN##", tokenMarkup));
    }
}
