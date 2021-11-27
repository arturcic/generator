using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace gen;

public record FieldInfo(string Type, string Name, string PropertyName);
public class GeneratorUtils
{
    private const string GeneratorFile = "AutoNotifyGenerator.sbntxt";
    private const string AttributeFile = "AutoNotifyAttribute.sbntxt";
    private static readonly string GeneratorContent = EmbeddedResource.GetContent(GeneratorFile);
    internal static readonly string AttributeContent = EmbeddedResource.GetContent(AttributeFile);

    internal static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is FieldDeclarationSyntax { AttributeLists.Count: > 0 };

    internal static FieldDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context) => (FieldDeclarationSyntax)context.Node;
    internal static string GenerateClassSource(ISymbol classSymbol, IEnumerable<IFieldSymbol> fields, ISymbol? attributeSymbol)
    {
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
        {
            return null; //TODO: issue a diagnostic that it must be top level
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var template = Template.Parse(GeneratorContent, GeneratorFile);
        var model = new
        {
            Namespace = namespaceName,
            ClassName = classSymbol.Name,
            Fields = fields.Select(x => GetFieldInfo(x, attributeSymbol)).ToArray()
        };
        return template.Render(model, member => member.Name);
    }
    private static string ChooseName(string fieldName, TypedConstant overridenNameOpt)
    {
        if (!overridenNameOpt.IsNull)
        {
            return overridenNameOpt.Value?.ToString();
        }

        fieldName = fieldName.TrimStart('_');
        return fieldName.Length switch
        {
            0 => string.Empty,
            1 => fieldName.ToUpper(),
            _ => fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1)
        };
    }
    private static FieldInfo GetFieldInfo(IFieldSymbol fieldSymbol, ISymbol? attributeSymbol)
    {
        // get the name and type of the field
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type;

        // get the AutoNotify attribute from the field, and any associated data
        var attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass != null && ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

        var propertyName = GeneratorUtils.ChooseName(fieldName, overridenNameOpt);
        return new FieldInfo(Name: fieldName, PropertyName: propertyName, Type: fieldType.ToDisplayString());
    }
}