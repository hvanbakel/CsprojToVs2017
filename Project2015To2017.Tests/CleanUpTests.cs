using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.CleanUp;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
	[TestClass]
	public class CleanUpTests
	{
		[TestMethod]
		public void CheckCleanUpExecution()
		{
			var project = new ProjectReader().Read(Path.Combine("Testfiles", "Transitive", "TransitiveDependeny.testcsproj"));
			var transformation = new FileTransformation();

			project.CodeFileExtension = "cs";
			transformation.Transform(project);

			var cleaner = new PackageReferenceCleaner(new DummyLogger());
			cleaner.CleanUpProjectReferences(project);

			Assert.AreEqual(1, project.PackageReferences.Count);
			Assert.AreEqual("Autofac.Wcf", project.PackageReferences[0].Id);
		}
	}
}