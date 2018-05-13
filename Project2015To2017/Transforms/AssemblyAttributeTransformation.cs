using System;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public class AssemblyAttributeTransformation : ITransformation
	{
		public void Transform(Project definition, IProgress<string> progress)
		{
			var attributeNodes = AssemblyAttributeNodes(definition.AssemblyAttributes);

			definition.AssemblyAttributeProperties = definition.AssemblyAttributeProperties
															   .Concat(attributeNodes)
															   .ToList()
															   .AsReadOnly();
		}

		private XElement[] AssemblyAttributeNodes(AssemblyAttributes assemblyAttributes)
		{
			if (assemblyAttributes == null)
			{
				return new XElement[0];
			}

			var versioningProperties = VersioningProperties(assemblyAttributes);

			var attributes = new[]
			{
				Tuple.Create("GenerateAssemblyTitleAttribute", assemblyAttributes.Title),
				Tuple.Create("GenerateAssemblyCompanyAttribute", assemblyAttributes.Company),
				Tuple.Create("GenerateAssemblyDescriptionAttribute", assemblyAttributes.Description),
				Tuple.Create("GenerateAssemblyProductAttribute", assemblyAttributes.Product),
				Tuple.Create("GenerateAssemblyCopyrightAttribute", assemblyAttributes.Copyright),
				Tuple.Create("GenerateAssemblyConfigurationAttribute", assemblyAttributes.Configuration)
			}.Concat(versioningProperties);

			var childNodes = attributes
								.Where(x => x.Item2 != null)
								.Select(x => new XElement(x.Item1, "false"))
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

		private static Tuple<string, string>[] VersioningProperties(AssemblyAttributes assemblyAttributes)
		{
			return new[]
			{
				Tuple.Create("GenerateAssemblyInformationalVersionAttribute", assemblyAttributes.InformationalVersion),
				Tuple.Create("GenerateAssemblyVersionAttribute", assemblyAttributes.Version),
				Tuple.Create("GenerateAssemblyFileVersionAttribute", assemblyAttributes.FileVersion)
			};
		}
	}
}