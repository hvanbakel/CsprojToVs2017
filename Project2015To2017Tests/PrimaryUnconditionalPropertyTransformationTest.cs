using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class PrimaryUnconditionalPropertyTransformationTest
	{
		[TestMethod]
		public void OutputAppendTargetFrameworkToOutputPathTrue()
		{
			var project = new Project
			{
				IsModernProject = true,
				AppendTargetFrameworkToOutputPath = true,
				PrimaryPropertyGroup = new XElement("PropertyGroup"),
				FilePath = new FileInfo("test.cs")
			};

			new PrimaryUnconditionalPropertyTransformation().Transform(project, NoopLogger.Instance);

			var appendTargetFrameworkToOutputPath = project.PrimaryPropertyGroup
				.Element("AppendTargetFrameworkToOutputPath");
			Assert.IsNull(appendTargetFrameworkToOutputPath);
		}

		[TestMethod]
		public void OutputAppendTargetFrameworkToOutputPathFalse()
		{
			var project = new Project
			{
				IsModernProject = true,
				AppendTargetFrameworkToOutputPath = false,
				PrimaryPropertyGroup = new XElement("PropertyGroup"),
				FilePath = new FileInfo("test.cs")
			};

			new PrimaryUnconditionalPropertyTransformation().Transform(project, NoopLogger.Instance);

			var appendTargetFrameworkToOutputPath = project.PrimaryPropertyGroup
				.Element("AppendTargetFrameworkToOutputPath");
			Assert.IsNotNull(appendTargetFrameworkToOutputPath);
			Assert.AreEqual("false", appendTargetFrameworkToOutputPath.Value);
		}
	}
}