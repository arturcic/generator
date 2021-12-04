using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;

namespace test;

public class TestsHelper
{
    public static Task TestIncrementalGenerator<TGenerator>(string code) where TGenerator : IIncrementalGenerator, new()
    {
        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CreateCompilation(code);
        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }
    
    public static Task TestSourceGenerator<TGenerator>(string code) where TGenerator : ISourceGenerator, new()
    {
        var generator = new TGenerator();
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