using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017;
using Project2015To2017.Definition;

namespace Project2015To2017Tests
{
	[TestClass]
    public class FileTransformationTest
    {
        [TestMethod]
        public async Task TransformsFiles()
        {
            var project = new Project();
            var transformation = new FileTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\fileinclusion.testcsproj");

	        var progress = new Progress<string>(x => { });

            await transformation.TransformAsync(doc, directoryInfo, project, progress).ConfigureAwait(false);

            Assert.AreEqual(5, project.ItemsToInclude.Count);

            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
            Assert.AreEqual(1, project.ItemsToInclude.Count(x => x.Name == nsSys + "Compile"));
            Assert.AreEqual(2, project.ItemsToInclude.Count(x => x.Name == "Compile"));
            Assert.AreEqual(2, project.ItemsToInclude.Count(x => x.Name == "Compile" && x.Attribute("Update") != null));
            Assert.AreEqual(0, project.ItemsToInclude.Count(x => x.Name == nsSys + "EmbeddedResource"));
            Assert.AreEqual(0, project.ItemsToInclude.Count(x => x.Name == nsSys + "Content"));
            Assert.AreEqual(2, project.ItemsToInclude.Count(x => x.Name == nsSys + "None"));
        }
    }
}
