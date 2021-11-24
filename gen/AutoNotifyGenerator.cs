using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace gen;

[Generator]
public class AutoNotifyGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterForPostInitialization(i =>
        {
            var content = EmbeddedResource.GetContent("AutoNotifyAttribute.sbntxt");
            i.AddSource("AutoNotifyAttribute", content);
        });

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
        foreach (var group in receiver.Fields.GroupBy(f => f.ContainingType))
        {
            var classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol);
            context.AddSource($"{group.Key.Name}.generated.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    private static string ProcessClass(ISymbol classSymbol, IEnumerable<IFieldSymbol> fields, ISymbol attributeSymbol)
    {
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
        {
            return null; //TODO: issue a diagnostic that it must be top level
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        const string file = "AutoNotifyGenerator.sbntxt";
        var template = Template.Parse(EmbeddedResource.GetContent(file), file);
        var model = new
        {
	        Namespace = namespaceName,
	        ClassName = classSymbol.Name,
	        Fields = fields.Select(x => GetFieldInfo(x, attributeSymbol)).ToArray()
        };
        var output = template.Render(model, member => member.Name);
        return output;
    }

    private static FieldInfo GetFieldInfo(IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
    {
	    // get the name and type of the field
	    var fieldName = fieldSymbol.Name;
	    var fieldType = fieldSymbol.Type;

	    // get the AutoNotify attribute from the field, and any associated data
	    var attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
	    var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

	    var propertyName = ChooseName(fieldName, overridenNameOpt);
	    return new FieldInfo
	    {
            Name = fieldName,
            PropertyName = propertyName,
            Type = fieldType.ToDisplayString()
	    };
    }

    private static string ChooseName(string fieldName, TypedConstant overridenNameOpt)
    {
	    if (!overridenNameOpt.IsNull)
	    {
		    return overridenNameOpt.Value.ToString();
	    }

	    fieldName = fieldName.TrimStart('_');
	    if (fieldName.Length == 0)
		    return string.Empty;

	    if (fieldName.Length == 1)
		    return fieldName.ToUpper();

	    return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
    }

    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    private class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IFieldSymbol> Fields { get; } = new();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // any field with at least one attribute is a candidate for property generation
            if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    // Get the symbol being declared by the field, and keep it if its annotated
                    var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"))
                    {
                        Fields.Add(fieldSymbol);
                    }
                }
            }
        }
    }

    private class FieldInfo
    {
	    public string Type { get; set; }
	    public string Name { get; set; }
	    public string PropertyName { get; set; }
    }
}