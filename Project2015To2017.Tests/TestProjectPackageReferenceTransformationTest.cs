using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
	[TestClass]
	public class TestProjectPackageReferenceTransformationTest
	{
		[TestMethod]
		public void AddsTestPackages()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));

			project.Type = ApplicationType.TestProject;
			project.TargetFrameworks.Add("net45");

			var transformation = new TestProjectPackageReferenceTransformation();

			transformation.Transform(project);

			Assert.AreEqual(10, project.PackageReferences.Count);
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && !string.IsNullOrWhiteSpace(x.Version)));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
		}

		[TestMethod]
		public void AcceptsNetStandardFramework()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));

			project.Type = ApplicationType.TestProject;
			project.TargetFrameworks.Add("netstandard2.0");

			var transformation = new TestProjectPackageReferenceTransformation();

			transformation.Transform(project);

			Assert.AreEqual(10, project.PackageReferences.Count);
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && !string.IsNullOrWhiteSpace(x.Version)));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
		}

		[TestMethod]
		public void DoesNotAddTestPackagesIfExists()
		{
			var transformation = new TestProjectPackageReferenceTransformation();

			var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "containsTestSDK.testcsproj"));

			project.TargetFrameworks.Add("net45");

			transformation.Transform(project);

			Assert.AreEqual(6, project.PackageReferences.Count);
			Assert.AreEqual(0, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter"));
		}

		[TestMethod]
		public void TransformsPackages()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));

			var transformation = new TestProjectPackageReferenceTransformation();

			transformation.Transform(project);

			Assert.AreEqual(7, project.PackageReferences.Count);
			Assert.AreEqual(2, project.PackageReferences.Count(x => x.IsDevelopmentDependency));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin" && x.Version == "3.1.0"));
		}

		[TestMethod]
		public void HandlesNonXml()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));
			var transformation = new TestProjectPackageReferenceTransformation();

			transformation.Transform(project);
		}

		[TestMethod]
		public void DoesNotAddDuplicateMSTestPackages()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "MSTestDuplicateTests", "existingMSTestPackages.testcsproj"));

			project.Type = ApplicationType.TestProject;
			project.TargetFrameworks.Add("net45");

			var transformation = new TestProjectPackageReferenceTransformation();

			// Check initial state
			Assert.AreEqual(2, project.PackageReferences.Count);
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter" && x.Version == "2.2.10"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestFramework" && x.Version == "2.2.10"));

			transformation.Transform(project);

			// Should only add Microsoft.NET.Test.Sdk since MSTest packages already exist
			Assert.AreEqual(3, project.PackageReferences.Count);
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter" && x.Version == "2.2.10"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestFramework" && x.Version == "2.2.10"));
		}

		[TestMethod]
		public void AddsOnlyMissingTestPackages()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "MSTestDuplicateTests", "partialMSTestPackages.testcsproj"));

			project.Type = ApplicationType.TestProject;
			project.TargetFrameworks.Add("net45");

			var transformation = new TestProjectPackageReferenceTransformation();

			// Check initial state - should have MSTest packages and another package
			Assert.AreEqual(3, project.PackageReferences.Count);
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter" && x.Version == "2.2.10"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestFramework" && x.Version == "2.2.10"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "SomeOtherPackage" && x.Version == "1.0.0"));

			transformation.Transform(project);

			// Should only add Microsoft.NET.Test.Sdk since MSTest packages already exist
			Assert.AreEqual(4, project.PackageReferences.Count);
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter" && x.Version == "2.2.10"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "MSTest.TestFramework" && x.Version == "2.2.10"));
			Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "SomeOtherPackage" && x.Version == "1.0.0"));
		}
	}
}
