using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017Tests
{
    [TestClass]
    public class AssemblyInfoTransformationTest
    {
        [TestMethod]
        public async Task FindsAttributesAsync()
        {
            var project = new Project { Type = ApplicationType.TestProject };
            var transformation = new AssemblyInfoTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.IsNotNull(project.AssemblyAttributes.Company);
            Assert.IsNotNull(project.AssemblyAttributes.Copyright);
            Assert.IsNotNull(project.AssemblyAttributes.InformationalVersion);
            Assert.IsNotNull(project.AssemblyAttributes.Product);
            Assert.AreEqual("Title", project.AssemblyAttributes.Title);
            Assert.IsNotNull(project.AssemblyAttributes.Version);
        }
    }
}
