using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class NugetPackageTransformationTest
	{
		[TestMethod]
		public void ConvertsNuspec()
		{
			var project = new ProjectReader()
								.Read("TestFiles\\net46console.testcsproj")
								.WithAssemblyAttributes(
									new AssemblyAttributes(
										assemblyName: "TestAssembly",
										informationalVersion: "7.0",
										copyright: "copyright from assembly",
										description: "description from assembly",
										company: "assembly author"
									)
								);

			var progress = new Progress<string>(x => { });
			var transformedProject = new NugetPackageTransformation().Transform(project, progress);

			var transformedPackageConfig = transformedProject.PackageConfiguration;

			Assert.IsNull(transformedPackageConfig.Id);
			Assert.IsNull(transformedPackageConfig.Version);
			Assert.AreEqual("some author", transformedPackageConfig.Authors);
			Assert.AreEqual("copyright from assembly", transformedPackageConfig.Copyright);
			Assert.IsTrue(transformedPackageConfig.RequiresLicenseAcceptance);
			Assert.AreEqual("a nice description.", transformedPackageConfig.Description);
			Assert.AreEqual("some tags API", transformedPackageConfig.Tags);
			Assert.AreEqual("someurl", transformedPackageConfig.LicenseUrl);
			Assert.AreEqual("Some long\n        text\n        with newlines", transformedPackageConfig.ReleaseNotes.Trim());
		}

		[TestMethod]
		public void ConvertsDependencies()
		{
			var project = new ProjectReader()
								.Read("TestFiles\\net46console.testcsproj")
								.WithPackageReferences(
									new[]
									{
										new PackageReference
										(
											id : "Newtonsoft.Json",
											version : "10.0.2"
										),
										new PackageReference
										(
											id : "Other.Package",
											version : "1.0.2"
										)
									}
								);

			var progress = new Progress<string>(x => { });

			var transformedProject = new NugetPackageTransformation().Transform(project, progress);

			Assert.AreEqual("[10.0.2,11)", transformedProject.PackageReferences.Single(x => x.Id == "Newtonsoft.Json").Version);
			Assert.AreEqual("1.0.2", transformedProject.PackageReferences.Single(x => x.Id == "Other.Package").Version);
		}
	}
}
