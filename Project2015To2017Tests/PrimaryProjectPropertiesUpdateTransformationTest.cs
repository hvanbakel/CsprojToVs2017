using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class PrimaryProjectPropertiesUpdateTransformationTest
	{
		[TestMethod]
		public void OutputAppendTargetFrameworkToOutputPathTrue()
		{
			var project = new Project
			{
				IsModernProject = true,
				AppendTargetFrameworkToOutputPath = true,
				PropertyGroups = new[] { new XElement("PropertyGroup") },
				FilePath = new FileInfo("test.cs")
			};

			new PrimaryProjectPropertiesUpdateTransformation().Transform(project);

			var appendTargetFrameworkToOutputPath = project.Property("AppendTargetFrameworkToOutputPath");
			Assert.IsNull(appendTargetFrameworkToOutputPath);
		}

		[TestMethod]
		public void OutputAppendTargetFrameworkToOutputPathFalse()
		{
			var project = new Project
			{
				IsModernProject = true,
				AppendTargetFrameworkToOutputPath = false,
				PropertyGroups = new[] { new XElement("PropertyGroup") },
				FilePath = new FileInfo("test.cs")
			};

			new PrimaryProjectPropertiesUpdateTransformation().Transform(project);

			var appendTargetFrameworkToOutputPath = project.Property("AppendTargetFrameworkToOutputPath");
			Assert.IsNotNull(appendTargetFrameworkToOutputPath);
			Assert.AreEqual("false", appendTargetFrameworkToOutputPath.Value);
		}
	}
}