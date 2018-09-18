using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Analysis.Diagnostics;
using Project2015To2017.Definition;
using Project2015To2017.Reading;

namespace Project2015To2017.Tests
{
	[TestClass]
	public class W001IllegalProjectTypeDiagnosticTest
	{
		[TestMethod]
		public void GeneratesResultForXamarinAndroid()
		{
			var project = new Project
			{
				ProjectDocument = new XDocument(
					new XElement("Project",
						new XElement(Project.XmlLegacyNamespace + "PropertyGroup",
							new XElement(
								XName.Get("ProjectTypeGuids", Project.XmlLegacyNamespace.NamespaceName),
								"{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"))))
			};

			ProjectPropertiesReader.ReadPropertyGroups(project);

			var results = new W001IllegalProjectTypeDiagnostic().Analyze(project);
			Assert.AreEqual(1, results.Count);
		}

		[TestMethod]
		public void DoesNotGenerateResultForValidType()
		{
			var project = new Project
			{
				ProjectDocument = new XDocument(
					new XElement("Project",
						new XElement(Project.XmlLegacyNamespace + "PropertyGroup",
							new XElement(
								XName.Get("ProjectTypeGuids", Project.XmlLegacyNamespace.NamespaceName),
								"{318C4C53-4319-472D-A480-6540F3D375FD}"))))
			};

			ProjectPropertiesReader.ReadPropertyGroups(project);

			var results = new W001IllegalProjectTypeDiagnostic().Analyze(project);
			Assert.IsNotNull(results);
			Assert.AreEqual(0, results.Count);
		}
	}
}