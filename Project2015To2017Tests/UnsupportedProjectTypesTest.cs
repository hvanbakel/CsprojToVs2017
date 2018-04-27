using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;

namespace Project2015To2017Tests
{
	[TestClass]
	public class UnsupportedProjectTypesTest
	{
		/// <summary>
		/// Run test cases
		/// </summary>
		/// <param name="guidTypes"></param>
		/// <param name="testCase"></param>
		/// <param name="expected"></param>
		[DataRow("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "ASP.NET 5", true)]
		[DataRow("{349C5851-65DF-11DA-9384-00065B846F21};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "ASP.NET MVC5", true)]
		[DataRow("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "C# only", false)]
		[DataRow("", "No ProjectTypeGuids tag", false)]
		[TestMethod]
		public void CheckAnUnsupportedProjectTypeReturnsCorrectResult(string guidTypes, string testCase, bool expected)
		{
			var xmlDocument = CreateTestProject(guidTypes);

			var actual = UnsupportedProjectTypes.IsUnsupportedProjectType(xmlDocument);

			Assert.AreEqual(expected, actual, $"Failed for {testCase}: expected {expected} but returned {actual}");
		}

		[TestMethod]
		public void IsUnsupportedProjectType_ThrowsExceptionIfXDocumentIsNull()
		{
			Assert.ThrowsException<ArgumentNullException>(() =>
			{
				UnsupportedProjectTypes.IsUnsupportedProjectType(null);
			});
		}

		/// <summary>
		/// Create a test case using the given param
		/// </summary>
		/// <param name="typeGuids"></param>
		/// <returns></returns>
		private static XDocument CreateTestProject(string typeGuids)
		{
			// parse empty template
			var xmlDocument = XDocument.Parse(template);
			if (!string.IsNullOrWhiteSpace(typeGuids))
			{
				XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
				var propertyGroup = xmlDocument.Descendants(nsSys+"PropertyGroup").First();
				propertyGroup.Add(new XElement(nsSys + "ProjectTypeGuids", typeGuids));
			}
			return xmlDocument;
		}

		/// <summary>
		/// Very basic proj template to create XDocument
		/// </summary>
		const string template = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
			"<Project ToolsVersion = \"15.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">" +
			"  <PropertyGroup>" +
			"    <ProjectGuid>{104B8196-D5BC-4901-B00C-FA065F5CEAD1}</ProjectGuid>" +
			"  </PropertyGroup>" +
			"</Project>";
	}
}
