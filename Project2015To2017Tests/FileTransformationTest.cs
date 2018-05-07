using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using static Project2015To2017.Definition.Project;

namespace Project2015To2017Tests
{
	[TestClass]
    public class FileTransformationTest
    {
        [TestMethod]
        public void TransformsFiles()
        {
            var project = new ProjectReader().Read("TestFiles\\Fileinclusion\\fileinclusion.testcsproj");
            var transformation = new FileTransformation();

	        var logEntries = new List<string>();

	        var progress = new Progress<string>(x => { logEntries.Add(x); });

            transformation.Transform(project, progress);

            Assert.AreEqual(6, project.IncludeItems.Count);

            Assert.AreEqual(1, project.IncludeItems.Count(x => x.Name == XmlNamespace + "Compile"));
            Assert.AreEqual(2, project.IncludeItems.Count(x => x.Name == "Compile"));
            Assert.AreEqual(2, project.IncludeItems.Count(x => x.Name == "Compile" && x.Attribute("Update") != null));
            Assert.AreEqual(1, project.IncludeItems.Count(x => x.Name == XmlNamespace + "EmbeddedResource")); // #73 inlcude things that are not ending in .resx
            Assert.AreEqual(0, project.IncludeItems.Count(x => x.Name == XmlNamespace + "Content"));
            Assert.AreEqual(2, project.IncludeItems.Count(x => x.Name == XmlNamespace + "None"));

	        var warningLogEntries = logEntries.Where(x => x.StartsWith("File found") || x.StartsWith("File was included"));

			Assert.IsFalse(warningLogEntries.Any());
        }
    }
}
