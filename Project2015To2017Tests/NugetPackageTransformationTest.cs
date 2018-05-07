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
								.Read("TestFiles\\OtherTestProjects\\net46console.testcsproj");

			project.AssemblyAttributes =
								new AssemblyAttributes {
									AssemblyName = "TestAssembly",
									InformationalVersion = "7.0",
									Copyright = "copyright from assembly",
									Description = "description from assembly",
									Company = "assembly author"
								};

			var progress = new Progress<string>(x => { });
			new NugetPackageTransformation().Transform(project, progress);

			var transformedPackageConfig = project.PackageConfiguration;

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
								.Read("TestFiles\\OtherTestProjects\\net46console.testcsproj");

			project.PackageReferences = new[]
										{
											new PackageReference
											{
												Id = "Newtonsoft.Json",
												Version = "10.0.2"
											},
											new PackageReference
											{
												Id = "Other.Package",
												Version = "1.0.2"
											}
										};

			var progress = new Progress<string>(x => { });

			new NugetPackageTransformation().Transform(project, progress);

			Assert.AreEqual("[10.0.2,11)", project.PackageReferences.Single(x => x.Id == "Newtonsoft.Json").Version);
			Assert.AreEqual("1.0.2", project.PackageReferences.Single(x => x.Id == "Other.Package").Version);
		}
	}
}
