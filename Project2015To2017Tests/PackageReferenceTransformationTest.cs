using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017Tests
{
    [TestClass]
    public class PackageReferenceTransformationTest
    {
        [TestMethod]
        public async Task AddsTestPackagesAsync()
        {
            var project = new Project { Type = ApplicationType.TestProject };
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(8, project.PackageReferences.Count);
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && x.Version == "15.0.0"));
        }

        [TestMethod]
        public async Task TransformsPackagesAsync()
        {
            var project = new Project();
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(5, project.PackageReferences.Count);
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.IsDevelopmentDependency));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin" && x.Version == "3.1.0"));
        }
    }
}
