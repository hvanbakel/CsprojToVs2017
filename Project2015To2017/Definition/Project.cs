using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public sealed class Project
	{
		public static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		public IList<AssemblyReference> AssemblyReferences { get; set; }
		public IList<ProjectReference> ProjectReferences { get; set; }
		public IList<PackageReference> PackageReferences { get; set; }
		public IList<XElement> IncludeItems { get; set; }
		public PackageConfiguration PackageConfiguration { get; set; }
		public AssemblyAttributes AssemblyAttributes { get; set; }
		public IList<XElement> AdditionalPropertyGroups { get; set; }
		public IList<XElement> Imports { get; set; }
		public IList<XElement> Targets { get; set; }
		public IList<XElement> BuildEvents { get; set; }
		public IList<string> Configurations { get; set; }

		public IList<string> TargetFrameworks { get; set; }
		public ApplicationType Type { get; set; }
		public bool Optimize { get; set; }
		public bool TreatWarningsAsErrors { get; set; }
		public string RootNamespace { get; set; }
		public string AssemblyName { get; set; }
		public bool AllowUnsafeBlocks { get; set; }
		public bool SignAssembly { get; set; }
		public string AssemblyOriginatorKeyFile { get; set; }
		public FileInfo FilePath { get; set; }
		public DirectoryInfo ProjectFolder => FilePath.Directory;
	}
}
