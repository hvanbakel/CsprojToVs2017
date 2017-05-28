using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017Tests
{
    [TestClass]
    public class ProjectReferenceTransformationTest
    {
        [TestMethod]
        public async Task TransformsProjectReferencesAsync()
        {
            var project = new Project();
            var transformation = new ProjectReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(2, project.ProjectReferences.Count);
            Assert.IsTrue(project.ProjectReferences.Contains(@"..\SomeOtherProject\SomeOtherProject.csproj"));
        }
    }
}
