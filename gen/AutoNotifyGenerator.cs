using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace gen;

public class AutoNotifyGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterForPostInitialization(i => i.AddSource("AutoNotifyAttribute", GeneratorUtils.AttributeContent));

        // Register a syntax receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // retrieve the populated receiver 
        if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
            return;

        // get the added attribute, and INotifyPropertyChanged
        var attributeSymbol = context.Compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");

        // group the fields by class, and generate the source
        foreach (var group in receiver.Candidates.GroupBy(f => f.ContainingType))
        {
            var classSource = GeneratorUtils.GenerateClassSource(group.Key, group.ToList(), attributeSymbol);
            context.AddSource($"{group.Key.Name}.generated.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    private class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IFieldSymbol> Candidates { get; } = new();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // any field with at least one attribute is a candidate for property generation
            if (!GeneratorUtils.IsSyntaxTargetForGeneration(context.Node)) return;

            var fieldDeclarationSyntax = GeneratorUtils.GetSemanticTargetForGeneration(context);
            foreach (var declaredSymbol in fieldDeclarationSyntax.Declaration.Variables.Select(variable => context.SemanticModel.GetDeclaredSymbol(variable)))
            {
                if (declaredSymbol is IFieldSymbol fieldSymbol && fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
                {
                    Candidates.Add(fieldSymbol);
                }
            }
        }
    }
}