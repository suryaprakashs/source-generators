using System.Collections.Generic;

namespace SourceGenerator.Model
{
    public class TypeModel
    {
        public string Name { get; set; }
        public string Classification { get; set; }
        public string Type { get; set; }
        public List<TypeModel> Children { get; set; }
    }
}