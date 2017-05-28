using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017Tests
{
    [TestClass]
    public class NugetPackageTransformationTest
    {
        [TestMethod]
        public async Task ConvertsNuspecAsync()
        {
            var project = new Project();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            project.AssemblyAttributes = new AssemblyAttributes
            {
                AssemblyName = "TestAssembly",
                InformationalVersion = "7.0",
                Copyright = "copyright from assembly",
                Description = "description from assembly",
                Company = "assembly author"
            };
            await new NugetPackageTransformation().TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.IsNull(project.PackageConfiguration.Id);
            Assert.IsNull(project.PackageConfiguration.Version);
            Assert.AreEqual("some author", project.PackageConfiguration.Authors);
            Assert.AreEqual("copyright from assembly", project.PackageConfiguration.Copyright);
            Assert.AreEqual(true, project.PackageConfiguration.RequiresLicenseAcceptance);
            Assert.AreEqual("a nice description.", project.PackageConfiguration.Description);
            Assert.AreEqual("some tags API", project.PackageConfiguration.Tags);
            Assert.AreEqual("someurl", project.PackageConfiguration.LicenseUrl);
            Assert.AreEqual("Some long\n        text\n        with newlines", project.PackageConfiguration.ReleaseNotes.Trim());
        }
    }
}
