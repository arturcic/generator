using System.Threading.Tasks;
using gen;
using VerifyXunit;
using Xunit;

namespace test;

[UsesVerify]
public class GeneratorTests
{
	[Fact]
	public async Task TestSourceGenerator()
	{
		const string code = @"
using AutoNotify;
namespace app
{
    public class ExampleViewModel
    {
        [AutoNotify]
        private string text = ""private field text"";

    }
}
";
		await TestsHelper.TestSourceGenerator<AutoNotifyGenerator>(code);
	}
	
	[Fact]
	public async Task TestIncrementalGenerator()
	{
		const string code = @"
using AutoNotify;
namespace app
{
    public class ExampleViewModel
    {
        [AutoNotify]
        private string text = ""private field text"";

    }
}
";
		await TestsHelper.TestIncrementalGenerator<AutoNotifyGenerator2>(code);
	}
}
