using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class AssemblyAttributeTransformationTest
	{
		private static AssemblyAttributes BaseAssemblyAttributes() =>
			new AssemblyAttributes
			{
				Company = "TheCompany Inc.",
				Configuration = "SomeConfiguration",
				Copyright = "A Copyright notice",
				Description = "A description notice",
				FileVersion = "1.1.7.9",
				InformationalVersion = "1.8.4.3-beta.1",
				Version = "1.0.4.2",
				Product = "The Product",
				Title = "The Title"
			};

		[TestMethod]
		public void MovesAttributesToCsProj()
		{
			var project = new Project
			{
				AssemblyAttributes = BaseAssemblyAttributes()
			};

			var transform = new AssemblyAttributeTransformation();

			transform.Transform(project, new Progress<string>());

			var expectedProperties = new[]
			{
				new XElement("GenerateAssemblyTitleAttribute", false),
				new XElement("GenerateAssemblyCompanyAttribute", false),
				new XElement("GenerateAssemblyDescriptionAttribute", false),
				new XElement("GenerateAssemblyProductAttribute", false),
				new XElement("GenerateAssemblyCopyrightAttribute", false),
				new XElement("GenerateAssemblyConfigurationAttribute", false),
				new XElement("GenerateAssemblyInformationalVersionAttribute", false),
				new XElement("GenerateAssemblyVersionAttribute", false),
				new XElement("GenerateAssemblyFileVersionAttribute", false)
			}
			.Select(x => x.ToString())
			.ToList();

			var actualProperties = project.AssemblyAttributeProperties
									      .Select(x => x.ToString())
										  .ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = BaseAssemblyAttributes();

			Assert.AreEqual(expectedAttributes, project.AssemblyAttributes);
		}
	}
}
