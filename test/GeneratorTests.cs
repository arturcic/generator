using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace test;

[UsesVerify]
public class GeneratorTests
{
	[Fact]
	public async Task Test()
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
		await TestsHelper.RunTest(code);
	}
}
