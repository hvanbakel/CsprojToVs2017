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
    public class AssemblyReferenceTransformationTest
    {
        [TestMethod]
        public async Task TransformsAssemblyReferencesAsync()
        {
            var project = new Project();
            var transformation = new AssemblyReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(8, project.AssemblyReferences.Count);
            Assert.IsTrue(project.AssemblyReferences.Contains(@"System.Xml.Linq"));
        }
    }
}
