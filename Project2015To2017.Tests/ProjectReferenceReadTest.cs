using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
	[TestClass]
	public class ProjectReferenceReadTest
	{
		[TestMethod]
		public void TransformsProjectReferences()
		{
			var project = new ProjectReader().Read(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj"));

			Assert.AreEqual(2, project.ProjectReferences.Count);
			Assert.IsTrue(project.ProjectReferences.Any(x => x.Include == @"..\SomeOtherProject\SomeOtherProject.csproj" && x.Aliases == "global,one"));
		}
	}
}
