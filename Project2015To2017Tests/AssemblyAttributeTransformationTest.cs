using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;
namespace Project2015To2017Tests
{
	[TestClass]
	public class AssemblyAttributeTransformationTest
	{
		private List<XElement> ProjectPropertyGroups = new [] {new XElement("PropertyGroup")}.ToList();

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
				Title = "The Title",
				File = new FileInfo("DummyAssemblyInfo.cs"),
				Trademark = "A trademark",
				Culture = "A culture",
				NeutralLanguage = "someLanguage"
			};

		[TestMethod]
		public void GenerateAssemblyInfoOnNothingSpecifiedTest()
		{
			var project = new Project
			{
				AssemblyAttributes = new AssemblyAttributes(),
				FilePath = new FileInfo("test.cs"),
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			var generateAssemblyInfo = project.AssemblyAttributeProperties.SingleOrDefault();
			Assert.IsNotNull(generateAssemblyInfo);
			Assert.AreEqual("GenerateAssemblyInfo", generateAssemblyInfo.Name);
			Assert.AreEqual("false", generateAssemblyInfo.Value);

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
		}

		[TestMethod]
		public void GenerateAssemblyInfoOption()
		{
			var project = new Project
			{
				AssemblyAttributes = BaseAssemblyAttributes(),
				FilePath = new FileInfo("test.cs"),
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance, true);

			transform.Transform(project);

			var generateAssemblyInfo = project.AssemblyAttributeProperties.SingleOrDefault();
			Assert.IsNotNull(generateAssemblyInfo);
			Assert.AreEqual("GenerateAssemblyInfo", generateAssemblyInfo.Name);
			Assert.AreEqual("false", generateAssemblyInfo.Value);

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
		}

		[TestMethod]
		public void MovesAttributesToCsProj()
		{
			var project = new Project
			{
				AssemblyAttributes = BaseAssemblyAttributes(),
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

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
				new XElement("FileVersion", "1.1.7.9"),
				new XElement("NeutralLanguage", "someLanguage"),
			}
			.Select(x => x.ToString())
			.ToList();

			var actualProperties = project.AssemblyAttributeProperties
										  .Select(x => x.ToString())
										  .ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = new AssemblyAttributes
			{
				Configuration = "SomeConfiguration",
				Trademark = "A trademark",
				Culture = "A culture"
			};

			Assert.IsTrue(expectedAttributes.Equals(project.AssemblyAttributes));

			CollectionAssert.AreEqual(project.AssemblyAttributeProperties.ToList(), project.PrimaryPropertyGroup().Elements().ToArray());

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
		}

		[TestMethod]
		public void BlankEntriesGetDeleted()
		{
			var baseAssemblyAttributes = BaseAssemblyAttributes();
			baseAssemblyAttributes.Company = "";
			baseAssemblyAttributes.Copyright = "";
			baseAssemblyAttributes.Description = "";
			baseAssemblyAttributes.Trademark = "";
			baseAssemblyAttributes.Culture = "";
			baseAssemblyAttributes.NeutralLanguage = "";

			var project = new Project
			{
				AssemblyAttributes = baseAssemblyAttributes,
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			var expectedProperties = new[]
				{
					new XElement("AssemblyTitle", "The Title"),
					new XElement("Product", "The Product"),
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

			Assert.IsTrue(expectedAttributes.Equals(project.AssemblyAttributes));

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
		}


		[TestMethod]
		public void BlankConfigurationGetsDeleted()
		{
			var assemblyAttributes = BaseAssemblyAttributes();
			assemblyAttributes.Configuration = "";

			var project = new Project
			{
				AssemblyAttributes = assemblyAttributes,
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			var expectedProperties = new[]
				{
					new XElement("AssemblyTitle", "The Title"),
					new XElement("Company", "TheCompany Inc."),
					new XElement("Description", "A description"),
					new XElement("Product", "The Product"),
					new XElement("Copyright", "A Copyright notice  ©"),
					new XElement("Version", "1.8.4.3-beta.1"),
					new XElement("AssemblyVersion", "1.0.4.2"),
					new XElement("FileVersion", "1.1.7.9"),
					new XElement("NeutralLanguage", "someLanguage")
				}
				.Select(x => x.ToString())
				.ToList();

			var actualProperties = project.AssemblyAttributeProperties
				.Select(x => x.ToString())
				.ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = new AssemblyAttributes();

			Assert.IsNull(project.AssemblyAttributes.Configuration);

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
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
				},
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

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

			Assert.IsTrue(expectedAttributes.Equals(project.AssemblyAttributes));

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
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
				},
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

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
					new XElement("FileVersion", "1.1.7.9"),
					new XElement("NeutralLanguage", "someLanguage")
				}
				.Select(x => x.ToString())
				.ToList();

			var actualProperties = project.AssemblyAttributeProperties
										  .Select(x => x.ToString())
										  .ToList();

			CollectionAssert.AreEquivalent(expectedProperties, actualProperties);

			var expectedAttributes = new AssemblyAttributes
			{
				Configuration = "SomeConfiguration",
				Trademark = "A trademark",
				Culture = "A culture"
			};

			Assert.IsTrue(expectedAttributes.Equals(project.AssemblyAttributes));
			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
		}

		[TestMethod]
		public void EmptyAssemblyInfoIsDeleted()
		{
			var project = new Project
			{
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var assemblyInfoFile = new FileInfo(@"TestFiles\AssemblyInfoHandling\Empty\Properties\AssemblyInfo.cs");
			project.AssemblyAttributes = new AssemblyAttributes
			{
				File = assemblyInfoFile
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			CollectionAssert.Contains(project.Deletions.ToList(), assemblyInfoFile);
		}

		[TestMethod]
		public void RedundantAssemblyInfoIsDeleted()
		{
			var project = new Project
			{
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var assemblyInfoFile = new FileInfo(@"TestFiles\AssemblyInfoHandling\Redundant\Properties\AssemblyInfo.cs");
			project.AssemblyAttributes = new AssemblyAttributes
			{
				File = assemblyInfoFile,
				FileContents = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(
									File.ReadAllText(assemblyInfoFile.FullName)
								).GetRoot()
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			CollectionAssert.Contains(project.Deletions.ToList(), assemblyInfoFile);
		}

		[TestMethod]
		public void ClassInAssemblyInfoIsNotDeleted()
		{
			var project = new Project
			{
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var assemblyInfoFile = new FileInfo(@"TestFiles\AssemblyInfoHandling\ClassDataLeft\Properties\AssemblyInfo.cs");
			project.AssemblyAttributes = new AssemblyAttributes
			{
				File = assemblyInfoFile,
				FileContents = (CompilationUnitSyntax)CSharpSyntaxTree.ParseText(
					File.ReadAllText(assemblyInfoFile.FullName)
				).GetRoot()
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			CollectionAssert.DoesNotContain(project.Deletions.ToList(), assemblyInfoFile);
		}

		[TestMethod]
		public void GenerateAssemblyInfoFromMultipleFilesTest()
		{
			var project = new Project
			{
				AssemblyAttributes = null,
				HasMultipleAssemblyInfoFiles = true,
				FilePath = new FileInfo("test.cs"),
				Deletions = new List<FileSystemInfo>(),
				PropertyGroups = ProjectPropertyGroups
			};

			var transform = new AssemblyAttributeTransformation(NoopLogger.Instance);

			transform.Transform(project);

			var generateAssemblyInfo = project.AssemblyAttributeProperties.SingleOrDefault();
			Assert.IsNotNull(generateAssemblyInfo);
			Assert.AreEqual("GenerateAssemblyInfo", generateAssemblyInfo.Name);
			Assert.AreEqual("false", generateAssemblyInfo.Value);

			CollectionAssert.DoesNotContain(project.Deletions?.ToList(), BaseAssemblyAttributes().File);
		}
	}
}
