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

	        var includeItems = project.IncludeItems;

	        Assert.AreEqual(9, includeItems.Count);

            Assert.AreEqual(4, includeItems.Count(x => x.Name == XmlNamespace + "Compile"));
            Assert.AreEqual(2, includeItems.Count(x => x.Name == "Compile"));
            Assert.AreEqual(6, includeItems.Count(x => (x.Name == "Compile" || x.Name == XmlNamespace + "Compile") && x.Attribute("Update") != null));
            Assert.AreEqual(1, includeItems.Count(x => x.Name == XmlNamespace + "EmbeddedResource")); // #73 inlcude things that are not ending in .resx
            Assert.AreEqual(0, includeItems.Count(x => x.Name == XmlNamespace + "Content"));
            Assert.AreEqual(2, includeItems.Count(x => x.Name == XmlNamespace + "None"));

	        var resourceDesigner = includeItems.Single(
								        x => x.Name == "Compile"
								             && x.Attribute("Update")?.Value == @"Properties\Resources.Designer.cs"
							        );

	        var dependentUponElement = resourceDesigner.Elements().Single();

			Assert.AreEqual("DependentUpon", dependentUponElement.Name);
			Assert.AreEqual("Resources.resx", dependentUponElement.Value);

	        var sourceWithDesigner = includeItems.Single(
								        x => x.Name == XmlNamespace + "Compile"
								             && x.Attribute("Update")?.Value == @"SourceFileWithDesigner.cs"
							        );

			var subTypeElement = sourceWithDesigner.Elements().Single();
	        Assert.AreEqual(XmlNamespace + "SubType", subTypeElement.Name);
	        Assert.AreEqual("Component", subTypeElement.Value);

	        var designerForSource = includeItems.Single(
								        x => x.Name == XmlNamespace + "Compile"
								             && x.Attribute("Update")?.Value == @"SourceFileWithDesigner.Designer.cs"
							        );

	        var dependentUponElement2 = designerForSource.Elements().Single();

	        Assert.AreEqual(XmlNamespace + "DependentUpon", dependentUponElement2.Name);
	        Assert.AreEqual("SourceFileWithDesigner.cs", dependentUponElement2.Value);

	        var fileWithAnotherAttribute = includeItems.Single(
												x => x.Name == XmlNamespace + "Compile"
													&& x.Attribute("Update")?.Value == @"AnotherFile.cs"
									        );

			Assert.AreEqual(2, fileWithAnotherAttribute.Attributes().Count());
			Assert.AreEqual("AttrValue", fileWithAnotherAttribute.Attribute("AnotherAttribute")?.Value);

				//<Compile Include="AnotherFile.cs" AnotherAttribute="AttrValue" />
	        var warningLogEntries = logEntries
								        .Where(x => x.StartsWith("File found") || x.StartsWith("File was included"))
								        //todo: would be good to be able to remove this condition and not warn on wildcard inclusions
										//that are covered by main wildcard
								        .Where(x => !x.ToUpper().Contains("WILDCARD"));

			Assert.IsFalse(warningLogEntries.Any());
        }
    }
}
