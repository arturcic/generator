using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using gen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        // Create the 'input' compilation that the generator will act on
        var inputCompilation = CreateCompilation(@"
namespace app
{
    public partial class ExampleViewModel
    {
        [AutoNotify]
        private string _text = ""private field text"";

        [AutoNotify(PropertyName = ""Count"")]
        private int _amount = 5;
    }
}
");

        // directly create an instance of the generator
        // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
        var generator = new AutoNotifyGenerator();

        // Create the driver that will control the generation, passing in our generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        // We can now assert things about the resulting compilation:
        Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
        Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
        Debug.Assert(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

        // Or we can look at the results directly:
        var runResult = driver.GetRunResult();

        // The runResult contains the combined results of all generators passed to the driver
        Debug.Assert(runResult.GeneratedTrees.Length == 1);
        Debug.Assert(runResult.Diagnostics.IsEmpty);

        // Or you can access the individual results on a by-generator basis
        var generatorResult = runResult.Results[0];
        Debug.Assert(generatorResult.Generator == generator);
        Debug.Assert(generatorResult.Diagnostics.IsEmpty);
        Debug.Assert(generatorResult.GeneratedSources.Length == 1);
        Debug.Assert(generatorResult.Exception is null);
        
        Console.WriteLine(generatorResult.GeneratedSources.First().SourceText);
    }

    private static Compilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}