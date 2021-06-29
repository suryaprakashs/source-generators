using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text.Json;
using SourceGenerator.Model;

namespace SourceGenerator
{
    [Generator]
    public class SourceProgram : ISourceGenerator
    {

        private const string Root = @"C:\workspace\neudesic\code-generators\application\templates";

        public void Execute(GeneratorExecutionContext context)
        {

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };

            var typeModels = JsonSerializer.Deserialize<List<TypeModel>>(File.ReadAllText(@"\context.json"), options);

            StringBuilder sourceBuilder = new StringBuilder();

            sourceBuilder.Append("using System;");
            sourceBuilder.Append("namespace SourceGenerator");
            sourceBuilder.Append("{");

            foreach (var typeModel in typeModels)
            {
                string data = GetMatchingType(typeModel);
                sourceBuilder.Append(data);
            }

            sourceBuilder.Append("}");


            // inject the created source into the users compilation
            context.AddSource("Source.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        private string GetMatchingType(TypeModel model)
        {
            string template = model.Classification switch
            {
                "Class" => File.ReadAllText(Path.Combine(Root, "class-template.tmpl")),
                "Property" => File.ReadAllText(Path.Combine(Root, "property-template.tmpl")),
                _ => ""
            };

            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            template = template.Replace("{Name}", model.Name);

            if (!string.IsNullOrEmpty(model.Type))
                template = template.Replace("{Type}", model.Type);

            StringBuilder sourceBuilder = new StringBuilder();

            if (model.Children?.Count > 0)
            {
                foreach (var child in model.Children)
                {
                    var children = GetMatchingType(child);
                    sourceBuilder.Append(children);
                }
            }

            template = template.Replace("{Children}", sourceBuilder.ToString());

            return template;
        }
    }
}
