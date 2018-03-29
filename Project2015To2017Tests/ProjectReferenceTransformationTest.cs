using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;
using Project2015To2017;

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
			
	        var progress = new Progress<string>(x => { });

            await transformation.TransformAsync(doc, directoryInfo, project, progress).ConfigureAwait(false);

            Assert.AreEqual(2, project.ProjectReferences.Count);
            Assert.IsTrue(project.ProjectReferences.Any(x => x.Include == @"..\SomeOtherProject\SomeOtherProject.csproj" && x.Aliases == "global,one"));
        }
    }
}
