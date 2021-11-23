using System.Reflection;
using System.Threading.Tasks;
using gen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;

namespace test;

public class TestsHelper
{
	public static Task RunTest(string code)
	{
		var generator = new AutoNotifyGenerator();
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		var compilation = CreateCompilation(code);

		driver = driver.RunGenerators(compilation);

		return Verifier.Verify(driver);
	}
	private static Compilation CreateCompilation(string source)
		=> CSharpCompilation.Create("compilation",
			new[] { CSharpSyntaxTree.ParseText(source) },
			new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
			new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}