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
				Copyright = "A Copyright notice  ©",
				Description = "A description",
				FileVersion = "1.1.7.9",
				InformationalVersion = "1.8.4.3-beta.1",
				Version = "1.0.4.2",
				Product = "The Product",
				Title = "The Title"
			};

		[TestMethod]
		public void GenerateAssemblyInfoOnNothingSpecifiedTest()
		{
			var project = new Project
			{
				AssemblyAttributes = new AssemblyAttributes(),
				FilePath = new System.IO.FileInfo("test.cs")
			};

			var transform = new AssemblyAttributeTransformation();

			transform.Transform(project, new Progress<string>());

			var generateAssemblyInfo = project.AssemblyAttributeProperties.SingleOrDefault();
			Assert.IsNotNull(generateAssemblyInfo);
			Assert.AreEqual("GenerateAssemblyInfo", generateAssemblyInfo.Name);
			Assert.AreEqual("false", generateAssemblyInfo.Value);
		}

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
				new XElement("AssemblyTitle", "The Title"),
				new XElement("Company", "TheCompany Inc."),
				new XElement("Description", "A description"),
				new XElement("Product", "The Product"),
				new XElement("Copyright", "A Copyright notice  ©"),
				new XElement("GenerateAssemblyConfigurationAttribute", false),
				new XElement("Version", "1.8.4.3-beta.1"),
				new XElement("AssemblyVersion", "1.0.4.2"),
				new XElement("FileVersion", "1.1.7.9")
			}
			.Select(x => x.ToString())
			.ToList();

			var actualProperties = project.AssemblyAttributeProperties
										  .Select(x => x.ToString())
										  .ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = new AssemblyAttributes
									{
										Configuration = "SomeConfiguration"
									};

			Assert.AreEqual(expectedAttributes, project.AssemblyAttributes);
		}

		[TestMethod]
		public void GeneratesAssemblyFileAttributeInCsProj()
		{
			var project = new Project
			{
				AssemblyAttributes = new AssemblyAttributes
				{
					InformationalVersion = "1.8.4.3-beta.1",
					//FileVersion should use this. In old projects, this happens automatically
					//but the converter needs to explicitly copy it
					Version = "1.0.4.2"
				}
			};

			var transform = new AssemblyAttributeTransformation();

			transform.Transform(project, new Progress<string>());

			var expectedProperties = new[]
				{
					new XElement("Version", "1.8.4.3-beta.1"),
					new XElement("AssemblyVersion", "1.0.4.2"),
					//Should be copied from assembly version
					new XElement("FileVersion", "1.0.4.2")
				}
				.Select(x => x.ToString())
				.ToList();

			var actualProperties = project.AssemblyAttributeProperties
				.Select(x => x.ToString())
				.ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = new AssemblyAttributes();

			Assert.AreEqual(expectedAttributes, project.AssemblyAttributes);
		}

		[TestMethod]
		public void PackagePropertiesOverrideAssemblyInfo()
		{
			var project = new Project
			{
				AssemblyAttributes = BaseAssemblyAttributes(),
				PackageConfiguration = new PackageConfiguration()
				{
					Copyright = "Some different copyright",
					Description = "Some other description",
					Version = "1.5.2-otherVersion"
				}
			};

			var transform = new AssemblyAttributeTransformation();

			transform.Transform(project, new Progress<string>());

			var expectedProperties = new[]
				{
					new XElement("AssemblyTitle", "The Title"),
					new XElement("Company", "TheCompany Inc."),
					new XElement("Description", "Some other description"),
					new XElement("Product", "The Product"),
					new XElement("Copyright", "Some different copyright"),
					new XElement("GenerateAssemblyConfigurationAttribute", false),
					new XElement("Version", "1.5.2-otherVersion"),
					new XElement("AssemblyVersion", "1.0.4.2"),
					new XElement("FileVersion", "1.1.7.9")
				}
				.Select(x => x.ToString())
				.ToList();

			var actualProperties = project.AssemblyAttributeProperties
										  .Select(x => x.ToString())
										  .ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = new AssemblyAttributes
			{
				Configuration = "SomeConfiguration"
			};

			Assert.AreEqual(expectedAttributes, project.AssemblyAttributes);
		}
	}
}
