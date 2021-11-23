﻿using System.CodeDom.Compiler;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace gen;

[Generator]
public class AutoNotifyGenerator : ISourceGenerator
{
    private const string AttributeText = @"
using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
";

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterForPostInitialization(i => i.AddSource("AutoNotifyAttribute", AttributeText));

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
        var notifySymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

        // group the fields by class, and generate the source
        foreach (var group in receiver.Fields.GroupBy(f => f.ContainingType))
        {
            var classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol);
            context.AddSource($"{group.Key.Name}.generated.cs", SourceText.From(classSource, Encoding.UTF8));
        }
    }

    private static string ProcessClass(ITypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol)
    {
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
        {
            return null; //TODO: issue a diagnostic that it must be top level
        }

        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        
        var model = new Model
        {
            Namespace = namespaceName,
            ClassName = classSymbol.Name,
        };

        // begin building the generated source
        var source = new IndentedTextWriter(new StringWriter());
        source.WriteLine("#nullable enable");
        source.WriteLine("using System.ComponentModel;");
        source.WriteLine($"namespace {namespaceName}");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"public partial class {classSymbol.Name} : INotifyPropertyChanged");
        source.WriteLine("{");
        source.Indent++;

        // if the class doesn't implement INotifyPropertyChanged already, add it
        if (!classSymbol.Interfaces.Contains(notifySymbol))
        {
            source.WriteLine("public event PropertyChangedEventHandler? PropertyChanged;");
        }

        // create properties for each field 
        foreach (var fieldSymbol in fields)
        {
            ProcessField(source, fieldSymbol, attributeSymbol);
        }

        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");

        // var output = template.Render(model, member => member.Name);

        var output = source.InnerWriter.ToString();
        return output;
    }

    private static void ProcessField(IndentedTextWriter source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
    {
        // get the name and type of the field
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type;

        // get the AutoNotify attribute from the field, and any associated data
        var attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

        var propertyName = ChooseName(fieldName, overridenNameOpt);
        if (propertyName.Length == 0 || propertyName == fieldName)
        {
            //TODO: issue a diagnostic that we can't process this field
            return;
        }
        
        source.WriteLine($"public {fieldType} {propertyName} ");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine("get");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"return this.{fieldName};");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("set");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"if (this.{fieldName} != value)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"this.{fieldName} = value;");
        source.WriteLine($"this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof({propertyName})));");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");

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

    private record Model
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
    }
}