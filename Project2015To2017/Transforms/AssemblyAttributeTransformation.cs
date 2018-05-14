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
			var otherProperties = OtherProperties(assemblyAttributes);

			var childNodes = otherProperties
								.Concat(versioningProperties)
								.ToArray();

			if (childNodes.Length == 0)
			{
				//Assume that the assembly info is coming from another file
				//which we don't have sight of so leave it up to consumer to
				//convert over if they wish
				return new[] { new XElement("GenerateAssemblyInfo", "false") };
			}
			else
			{
				return childNodes;
			}
		}

		private static IEnumerable<XElement> OtherProperties(AssemblyAttributes assemblyAttributes)
		{
			var toReturn = Properties()
								.Where(x => x != null)
								.ToList();

			assemblyAttributes.Title = null;
			assemblyAttributes.Company = null;
			assemblyAttributes.Description = null;
			assemblyAttributes.Product = null;
			assemblyAttributes.Copyright = null;

			return toReturn;

			IEnumerable<XElement> Properties()
			{
				yield return XElement(assemblyAttributes.Title, "AssemblyTitle");
				yield return XElement(assemblyAttributes.Company, "Company");
				yield return XElement(assemblyAttributes.Description, "Description");
				yield return XElement(assemblyAttributes.Product, "Product");
				yield return XElement(assemblyAttributes.Copyright, "Copyright");

				if (assemblyAttributes.Configuration != null)
				{
					//If it is included, chances are that the developer has used
					//preprocessor flags which we can't yet process
					//so just leave it in AssemblyInfo file
					yield return new XElement("GenerateAssemblyConfigurationAttribute", false);
				}
			}
		}

		private static List<XElement> VersioningProperties(AssemblyAttributes assemblyAttributes)
		{
			var toReturn = Properties()
								.Where(x => x != null)
								.ToList();

			assemblyAttributes.InformationalVersion = null;
			assemblyAttributes.Version = null;
			assemblyAttributes.FileVersion = null;

			return toReturn;

			IEnumerable<XElement> Properties()
			{
				yield return XElement(assemblyAttributes.InformationalVersion, "Version");
				yield return XElement(assemblyAttributes.Version, "AssemblyVersion");

				//The AssemblyInfo behaviour was to fallback on the AssemblyVersion for the file version
				//but in the new format, this doesn't happen so we explicitly copy the value across
				yield return XElement(assemblyAttributes.FileVersion, "FileVersion") ??
				             XElement(assemblyAttributes.Version, "FileVersion");
			}
		}

		private static XElement XElement(string attribute, string name)
		{
			if (attribute != null)
			{
				return new XElement(name, attribute);
			}
			else
			{
				return null;
			}
		}
	}
}