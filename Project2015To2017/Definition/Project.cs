using System.Collections.Generic;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
    internal sealed class Project
    {
        public List<AssemblyReference> AssemblyReferences { get; internal set; }
        public IReadOnlyList<ProjectReference> ProjectReferences { get; internal set; }
        public IReadOnlyList<PackageReference> PackageReferences { get; internal set; }
        public IReadOnlyList<XElement> ItemsToInclude { get; internal set; }
        public PackageConfiguration PackageConfiguration { get; internal set; }
        public AssemblyAttributes AssemblyAttributes { get; internal set; }
        public IReadOnlyList<XElement> ConditionalPropertyGroups { get; internal set; }

        public IReadOnlyList<string> TargetFrameworks { get; internal set; }
        public ApplicationType Type { get; internal set; }
        public bool Optimize { get; internal set; }
        public bool TreatWarningsAsErrors { get; internal set; }
        public string RootNamespace { get; internal set; }
        public string AssemblyName { get; internal set; }
        public bool AllowUnsafeBlocks { get; internal set; }
        public bool SignAssembly { get; internal set; }
        public string AssemblyOriginatorKeyFile { get; internal set; }
    }
}
