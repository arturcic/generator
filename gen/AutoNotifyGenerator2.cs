using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace gen;

[Generator(LanguageNames.CSharp)]
public class AutoNotifyGenerator2 : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("AutoNotifyAttribute", GeneratorUtils.AttributeContent));
        var fieldDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => GeneratorUtils.IsSyntaxTargetForGeneration(s), 
                static (ctx, _) => GeneratorUtils.GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        var compilationAndFields = context.CompilationProvider.Combine(fieldDeclarations.Collect());
        
        context.RegisterSourceOutput(compilationAndFields, static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }
    
    private static void Execute(Compilation compilation, ImmutableArray<FieldDeclarationSyntax> fields, SourceProductionContext context)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
        List<IFieldSymbol> candidates = new();
        foreach (var fieldDeclarationSyntax in fields)
        {
            foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
            {
                // Get the symbol being declared by the field, and keep it if its annotated with AutoNotify
                var semanticModel = compilation.GetSemanticModel(variable.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol
                    && fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == attributeSymbol.ToDisplayString()))
                {
                    candidates.Add(fieldSymbol);
                }
            }    
        }
        
        foreach (var group in candidates.GroupBy(f => f.ContainingType))
        {
            var classSource = GeneratorUtils.GenerateClassSource(group.Key, group.ToList(), attributeSymbol);
            context.AddSource($"{group.Key.Name}.generated2.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }
}