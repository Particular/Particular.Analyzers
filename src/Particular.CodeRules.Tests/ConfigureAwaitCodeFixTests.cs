using System.Threading.Tasks;
using Particular.CodeRules.ConfigureAwait;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class ConfigureAwaitCodeFixTests : CSharpCodeFixTestFixture<ConfigureAwaitCodeFix>
    {
        [Fact]
        public Task AddConfigureAwaitTrue()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public async Task Foo()
    {
        await [|Task.Delay(1)|];
    }
}";
            const string expected = @"
using System.Threading.Tasks;
class C
{
    public async Task Foo()
    {
        await Task.Delay(1).ConfigureAwait(true);
    }
}";

            return TestCodeFix(code, expected, DiagnosticDescriptors.UseConfigureAwait, 2, 0);
        }

        [Fact]
        public Task AddConfigureAwaitFalse()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public async Task Foo()
    {
        await [|Task.Delay(1)|];
    }
}";
            const string expected = @"
using System.Threading.Tasks;
class C
{
    public async Task Foo()
    {
        await Task.Delay(1).ConfigureAwait(false);
    }
}";

            return TestCodeFix(code, expected, DiagnosticDescriptors.UseConfigureAwait, 2, 1);
        }
    }
}