using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Project2015To2017Tests
{
	[TestClass]
    public class W001IllegalProjectTypeDiagnosticTest
    {
		[TestMethod]
		public void GeneratesResultForXamarinAndroid()
		{
			var project = new Project2015To2017.Definition.Project
			{
				ProjectDocument = new XDocument(
					new XElement("Project",
						new XElement(XName.Get("ProjectTypeGuids", Project2015To2017.Definition.Project.XmlLegacyNamespace.NamespaceName), "{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}")))
			};

			var results = new Project2015To2017.Analysis.Diagnostics.W001IllegalProjectTypeDiagnostic().Analyze(project);
			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void DoesNotGenerateResultForValidType()
		{
			var project = new Project2015To2017.Definition.Project
			{
				ProjectDocument = new XDocument(
					new XElement("Project",
						new XElement(XName.Get("ProjectTypeGuids", Project2015To2017.Definition.Project.XmlLegacyNamespace.NamespaceName), "{318C4C53-4319-472D-A480-6540F3D375FD}")))
			};

			var results = new Project2015To2017.Analysis.Diagnostics.W001IllegalProjectTypeDiagnostic().Analyze(project);
			Assert.IsNotNull(results);
			Assert.AreEqual(0, results.Count);
		}
	}
}
