using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Reading;
using System.IO;

namespace Project2015To2017Tests
{
	[TestClass]
    public class AssemblyInfoReadTest
    {
        [TestMethod]
        public void FindsAttributes()
        {
	        var project = new ProjectReader(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj")).Read();

            Assert.IsNotNull(project.AssemblyAttributes.Company);
            Assert.IsNotNull(project.AssemblyAttributes.Copyright);
            Assert.IsNotNull(project.AssemblyAttributes.InformationalVersion);
            Assert.IsNotNull(project.AssemblyAttributes.Product);
            Assert.AreEqual("Title", project.AssemblyAttributes.Title);
            Assert.IsNotNull(project.AssemblyAttributes.Version);
        }
    }
}
