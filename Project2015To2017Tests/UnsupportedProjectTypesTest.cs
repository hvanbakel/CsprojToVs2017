using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;

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
			var xmlDocument = CreateTestProject("ProjectTypeGuids", guidTypes);

			var actual = UnsupportedProjectTypes.IsUnsupportedProjectType(xmlDocument);

			Assert.AreEqual(expected, actual, $"Failed for {testCase}: expected {expected} but returned {actual}");
		}

		/// <summary>
		/// Run test cases
		/// </summary>
		/// <param name="guidTypes"></param>
		/// <param name="testCase"></param>
		/// <param name="expected"></param>
		[DataRow("WindowsForms", "WinForms", false)]
		[DataRow("", "Other", false)]
		[TestMethod]
		public void CheckAnUnsupportedProjectOutputReturnsCorrectResult(string outputType, string testCase, bool expected)
		{
			var xmlDocument = CreateTestProject("MyType", outputType);

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
		/// Create a test case using the given element name + value
		/// </summary>
		/// <param name="elementName">element name to add to PropertyGroup</param>
		/// <param name="value">value to set</param>
		/// <returns></returns>
		private static Project CreateTestProject(string elementName, string value)
		{
			// parse empty template
			var xmlDocument = XDocument.Parse(template);
			if (!string.IsNullOrWhiteSpace(value))
			{
				var propertyGroup = xmlDocument.Descendants(Project.XmlLegacyNamespace + "PropertyGroup").First();
				propertyGroup.Add(new XElement(Project.XmlLegacyNamespace + elementName, value));
			}
			return new Project { ProjectDocument = xmlDocument };
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
