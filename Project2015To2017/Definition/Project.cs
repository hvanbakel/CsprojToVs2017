using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public sealed class Project
	{
		public static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		public IReadOnlyList<AssemblyReference> AssemblyReferences { get; set; }
		public IReadOnlyList<ProjectReference> ProjectReferences { get; set; }
		public IReadOnlyList<PackageReference> PackageReferences { get; set; }
		public IReadOnlyList<XElement> IncludeItems { get; set; }
		public PackageConfiguration PackageConfiguration { get; set; }
		public AssemblyAttributes AssemblyAttributes { get; set; }
		public IReadOnlyList<XElement> AdditionalPropertyGroups { get; set; }
		public IReadOnlyList<XElement> Imports { get; set; }
		public IReadOnlyList<XElement> Targets { get; set; }
		public IReadOnlyList<XElement> BuildEvents { get; set; }
		public IReadOnlyList<string> Configurations { get; set; }
		public IReadOnlyList<XElement> OtherPropertyGroups { get; set; }

		public IReadOnlyList<string> TargetFrameworks { get; set; }
		public ApplicationType Type { get; set; }
		public bool Optimize { get; set; }
		public bool TreatWarningsAsErrors { get; set; }
		public string RootNamespace { get; set; }
		public string AssemblyName { get; set; }
		public bool AllowUnsafeBlocks { get; set; }
		public bool SignAssembly { get; set; }
		public bool? DelaySign { get; internal set; }
		public string AssemblyOriginatorKeyFile { get; set; }
		public FileInfo FilePath { get; set; }
		public DirectoryInfo ProjectFolder => FilePath.Directory;
	}
}
