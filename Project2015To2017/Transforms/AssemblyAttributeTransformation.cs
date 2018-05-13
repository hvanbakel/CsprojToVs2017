using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public class AssemblyAttributeTransformation : ITransformation
	{
		public void Transform(Project definition, IProgress<string> progress)
		{
			var attributeNodes = AssemblyAttributeNodes(definition.AssemblyAttributes, progress);

			definition.AssemblyAttributeProperties = definition.AssemblyAttributeProperties
															   .Concat(attributeNodes)
															   .ToList()
															   .AsReadOnly();
		}

		private static XElement[] AssemblyAttributeNodes(AssemblyAttributes assemblyAttributes, IProgress<string> progress)
		{
			if (assemblyAttributes == null)
			{
				return new XElement[0];
			}

			progress.Report("Moving attributes from AssemblyInfo to project file");

			var versioningProperties = VersioningProperties(assemblyAttributes);

			var attributes = new[]
			{
				Tuple.Create("GenerateAssemblyTitleAttribute", assemblyAttributes.Title),
				Tuple.Create("GenerateAssemblyCompanyAttribute", assemblyAttributes.Company),
				Tuple.Create("GenerateAssemblyDescriptionAttribute", assemblyAttributes.Description),
				Tuple.Create("GenerateAssemblyProductAttribute", assemblyAttributes.Product),
				Tuple.Create("GenerateAssemblyCopyrightAttribute", assemblyAttributes.Copyright),
				Tuple.Create("GenerateAssemblyConfigurationAttribute", assemblyAttributes.Configuration)
			};

			var childNodes = attributes
								.Where(x => x.Item2 != null)
								.Select(x => new XElement(x.Item1, "false"))
								.Concat(versioningProperties)
								.ToArray();

			if (childNodes.Length == 0)
			{
				//Assume that the assembly info is coming from another file
				//which we don't have sight of so leave it up to consumer to
				//convert over if they wish
				return new [] {new XElement("GenerateAssemblyInfo", "false")};
			}
			else
			{
				return childNodes;
			}
		}

		private static IEnumerable<XElement> VersioningProperties(AssemblyAttributes assemblyAttributes)
		{
			if (assemblyAttributes.InformationalVersion != null)
			{
				yield return new XElement("Version", assemblyAttributes.InformationalVersion);
			}

			if (assemblyAttributes.Version != null)
			{
				yield return new XElement("AssemblyVersion", assemblyAttributes.Version);
			}

			if (assemblyAttributes.FileVersion != null)
			{
				yield return new XElement("FileVersion", assemblyAttributes.FileVersion);
			}
			//The AssemblyInfo behaviour was to fallback on the AssemblyVersion for the file version
			//but in the new format, this doesn't happen so we explicitly copy the value across
			else if (assemblyAttributes.Version != null)
			{
				yield return new XElement("FileVersion", assemblyAttributes.Version);
			}

			assemblyAttributes.InformationalVersion = null;
			assemblyAttributes.Version = null;
			assemblyAttributes.FileVersion = null;
		}
	}
}