using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text.Json;
using SourceGenerator.Model;
using System.Collections.Immutable;
using System.Threading;
using System.Linq;
using System;

namespace SourceGenerator
{

    [Generator]
    public class SourceProgram : ISourceGenerator
    {

        static DiagnosticDescriptor InvalidXmlWarning = new DiagnosticDescriptor(id: "CUSTOM001",
                                                                                title: "Couldn't parse XML file",
                                                                                messageFormat: "Couldn't parse XML file '{0}'.",
                                                                                category: "MyGenerator",
                                                                                DiagnosticSeverity.Warning,
                                                                                isEnabledByDefault: true);

        public void Execute(GeneratorExecutionContext context)
        {

            context.ReportDiagnostic(Diagnostic.Create(InvalidXmlWarning, Location.None, "."));

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };

            var userContext = LoadFile(context.AdditionalFiles, context.CancellationToken, "context.json");
            var typeModels = JsonSerializer.Deserialize<List<TypeModel>>(userContext, options);

            StringBuilder sourceBuilder = new StringBuilder();

            sourceBuilder.Append("using System;");
            sourceBuilder.Append("namespace SourceGenerator");
            sourceBuilder.Append("{");

            foreach (var typeModel in typeModels)
            {
                string source = GetMatchingTypeSource(typeModel, context);
                sourceBuilder.Append(source);
            }

            sourceBuilder.Append("}");

            var sourceText = SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
            // inject the created source into the users compilation
            context.AddSource("Source.cs", sourceText);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        private static string LoadFile(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken, string fileName)
        {
            var file = additionalFiles.SingleOrDefault(f => string.Compare(Path.GetFileName(f.Path), fileName, StringComparison.OrdinalIgnoreCase) == 0);
            if (file == null)
            {
                return null;
            }

            var fileText = file.GetText(cancellationToken);

            using (var stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                {
                    fileText.Write(writer, cancellationToken);
                }

                stream.Position = 0;

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private string GetMatchingTypeSource(TypeModel model, GeneratorExecutionContext context)
        {
            string template = GetTemplate(model.Classification, context);

            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            template = template.Replace("{Name}", model.Name);

            if (!string.IsNullOrEmpty(model.Type))
            {
                template = template.Replace("{Type}", model.Type);
            }

            StringBuilder sourceBuilder = new StringBuilder();

            if (model.Children?.Count > 0)
            {
                foreach (var child in model.Children)
                {
                    var children = GetMatchingTypeSource(child, context);
                    sourceBuilder.Append(children);
                }
            }

            template = template.Replace("{Children}", sourceBuilder.ToString());

            return template;
        }

        private static string GetTemplate(string classification, GeneratorExecutionContext context)
        {
            return classification switch
            {
                "Class" => LoadFile(context.AdditionalFiles, context.CancellationToken, "class-template.tmpl"),
                "Property" => LoadFile(context.AdditionalFiles, context.CancellationToken, "property-template.tmpl"),
                _ => ""
            };
        }
    }
}
