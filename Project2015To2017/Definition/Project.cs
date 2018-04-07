using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public sealed class Project
	{
		public static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

		public Project(IReadOnlyList<AssemblyReference> assemblyReferences,
						IReadOnlyList<ProjectReference> projectReferences,
						IReadOnlyList<PackageReference> packageReferences,
						IReadOnlyList<XElement> includeItems,
						PackageConfiguration packageConfiguration,
						AssemblyAttributes assemblyAttributes,
						IReadOnlyList<XElement> additionalPropertyGroups,
						IReadOnlyList<XElement> imports, IReadOnlyList<XElement> targets,
						IReadOnlyList<XElement> buildEvents, IReadOnlyList<string> configurations,
						IReadOnlyList<string> targetFrameworks, ApplicationType type,
						bool optimize, bool treatWarningsAsErrors, string rootNamespace,
						string assemblyName, bool allowUnsafeBlocks, bool signAssembly,
						string assemblyOriginatorKeyFile, FileInfo filePath)
		{
			AssemblyReferences = assemblyReferences;
			ProjectReferences = projectReferences;
			PackageReferences = packageReferences;
			IncludeItems = includeItems;
			PackageConfiguration = packageConfiguration;
			AssemblyAttributes = assemblyAttributes;
			AdditionalPropertyGroups = additionalPropertyGroups;
			Imports = imports;
			Targets = targets;
			BuildEvents = buildEvents;
			Configurations = configurations;
			TargetFrameworks = targetFrameworks;
			Type = type;
			Optimize = optimize;
			TreatWarningsAsErrors = treatWarningsAsErrors;
			RootNamespace = rootNamespace;
			AssemblyName = assemblyName;
			AllowUnsafeBlocks = allowUnsafeBlocks;
			SignAssembly = signAssembly;
			AssemblyOriginatorKeyFile = assemblyOriginatorKeyFile;
			FilePath = filePath;
		}

		public IReadOnlyList<AssemblyReference> AssemblyReferences { get; }
		public IReadOnlyList<ProjectReference> ProjectReferences { get; }
		public IReadOnlyList<PackageReference> PackageReferences { get; }
		public IReadOnlyList<XElement> IncludeItems { get; }
		public PackageConfiguration PackageConfiguration { get; }
		public AssemblyAttributes AssemblyAttributes { get; }
		public IReadOnlyList<XElement> AdditionalPropertyGroups { get; }
		public IReadOnlyList<XElement> Imports { get; }
		public IReadOnlyList<XElement> Targets { get; }
		public IReadOnlyList<XElement> BuildEvents { get; }
		public IReadOnlyList<string> Configurations { get; }

		public IReadOnlyList<string> TargetFrameworks { get; }
		public ApplicationType Type { get; }
		public bool Optimize { get; }
		public bool TreatWarningsAsErrors { get; }
		public string RootNamespace { get; }
		public string AssemblyName { get; }
		public bool AllowUnsafeBlocks { get; }
		public bool SignAssembly { get; }
		public string AssemblyOriginatorKeyFile { get; }
		public FileInfo FilePath { get; }
		public DirectoryInfo ProjectFolder => FilePath.Directory;

		public Project WithAssemblyReferences(IEnumerable<AssemblyReference> assemblyReferences)
		{
			return new Project(
						assemblyReferences.ToList().AsReadOnly(),
						ProjectReferences,
						PackageReferences, IncludeItems,
						PackageConfiguration, AssemblyAttributes,
						AdditionalPropertyGroups, Imports,
						Targets, BuildEvents, Configurations,
						TargetFrameworks, Type, Optimize,
						TreatWarningsAsErrors, RootNamespace,
						AssemblyName, AllowUnsafeBlocks,
						SignAssembly, AssemblyOriginatorKeyFile,
						FilePath
					);
		}

		public Project WithIncludeItems(IReadOnlyList<XElement> items)
		{
			return new Project(
				AssemblyReferences,
				ProjectReferences,
				PackageReferences, items,
				PackageConfiguration, AssemblyAttributes,
				AdditionalPropertyGroups, Imports,
				Targets, BuildEvents, Configurations,
				TargetFrameworks, Type, Optimize,
				TreatWarningsAsErrors, RootNamespace,
				AssemblyName, AllowUnsafeBlocks,
				SignAssembly, AssemblyOriginatorKeyFile,
				FilePath
			);
		}

		public Project WithPackageConfig(PackageConfiguration packageConfig)
		{
			return new Project(
				AssemblyReferences,
				ProjectReferences,
				PackageReferences, IncludeItems,
				packageConfig, AssemblyAttributes,
				AdditionalPropertyGroups, Imports,
				Targets, BuildEvents, Configurations,
				TargetFrameworks, Type, Optimize,
				TreatWarningsAsErrors, RootNamespace,
				AssemblyName, AllowUnsafeBlocks,
				SignAssembly, AssemblyOriginatorKeyFile,
				FilePath
			);
		}

		public Project WithPackageReferences(IReadOnlyList<PackageReference> packageReferences)
		{
			return new Project(
				AssemblyReferences,
				ProjectReferences,
				packageReferences, IncludeItems,
				PackageConfiguration, AssemblyAttributes,
				AdditionalPropertyGroups, Imports,
				Targets, BuildEvents, Configurations,
				TargetFrameworks, Type, Optimize,
				TreatWarningsAsErrors, RootNamespace,
				AssemblyName, AllowUnsafeBlocks,
				SignAssembly, AssemblyOriginatorKeyFile,
				FilePath
			);
		}

		public Project WithType(ApplicationType type)
		{
			return new Project(
				AssemblyReferences,
				ProjectReferences,
				PackageReferences, IncludeItems,
				PackageConfiguration, AssemblyAttributes,
				AdditionalPropertyGroups, Imports,
				Targets, BuildEvents, Configurations,
				TargetFrameworks, type, Optimize,
				TreatWarningsAsErrors, RootNamespace,
				AssemblyName, AllowUnsafeBlocks,
				SignAssembly, AssemblyOriginatorKeyFile,
				FilePath
			);
		}

		public Project WithTargetFrameworks(string[] targetFrameworks)
		{
			return new Project(
				AssemblyReferences,
				ProjectReferences,
				PackageReferences, IncludeItems,
				PackageConfiguration, AssemblyAttributes,
				AdditionalPropertyGroups, Imports,
				Targets, BuildEvents, Configurations,
				targetFrameworks, Type, Optimize,
				TreatWarningsAsErrors, RootNamespace,
				AssemblyName, AllowUnsafeBlocks,
				SignAssembly, AssemblyOriginatorKeyFile,
				FilePath
			);
		}

		public Project WithAssemblyAttributes(AssemblyAttributes assemblyAttributes)
		{
			return new Project(
				AssemblyReferences,
				ProjectReferences,
				PackageReferences, IncludeItems,
				PackageConfiguration, assemblyAttributes,
				AdditionalPropertyGroups, Imports,
				Targets, BuildEvents, Configurations,
				TargetFrameworks, Type, Optimize,
				TreatWarningsAsErrors, RootNamespace,
				AssemblyName, AllowUnsafeBlocks,
				SignAssembly, AssemblyOriginatorKeyFile,
				FilePath
			);
		}
	}
}
