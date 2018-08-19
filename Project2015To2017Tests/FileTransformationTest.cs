using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
    public class FileTransformationTest
    {
        [TestMethod]
        public void TransformsFiles()
        {
            var project = new ProjectReader(Path.Combine("TestFiles", "FileInclusion", "fileinclusion.testcsproj")).Read();
            var transformation = new FileTransformation();

	        var progress = new Progress<string>();

            transformation.Transform(project, progress);

	        var includeItems = project.ItemGroups.SelectMany(x => x.Elements()).ToImmutableList();

	        Assert.AreEqual(29, includeItems.Count);

            Assert.AreEqual(12, includeItems.Count(x => x.Name.LocalName == "Reference"));
            Assert.AreEqual(2, includeItems.Count(x => x.Name.LocalName == "ProjectReference"));
            Assert.AreEqual(11, includeItems.Count(x => x.Name.LocalName == "Compile"));
			Assert.AreEqual(6, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Update") != null));
			Assert.AreEqual(3, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Include") != null));
			Assert.AreEqual(2, includeItems.Count(x => x.Name.LocalName == "Compile" && x.Attribute("Remove") != null));
			Assert.AreEqual(2, includeItems.Count(x => x.Name.LocalName == "EmbeddedResource")); // #73 inlcude things that are not ending in .resx
            Assert.AreEqual(0, includeItems.Count(x => x.Name.LocalName == "Content"));
            Assert.AreEqual(2, includeItems.Count(x => x.Name.LocalName == "None"));
            Assert.AreEqual(0, includeItems.Count(x => x.Name.LocalName == "Analyzer"));

	        var resourceDesigner = includeItems.Single(
		        x => x.Name.LocalName == "Compile"
		             && x.Attribute("Update")?.Value == @"Properties\Resources.Designer.cs"
	        );

			Assert.AreEqual(3, resourceDesigner.Elements().Count());
	        var dependentUponElement = resourceDesigner.Elements().Single(x => x.Name.LocalName == "DependentUpon");

			Assert.AreEqual("Resources.resx", dependentUponElement.Value);

	        var linkedFile = includeItems.Single(
		        x => x.Name.LocalName == "Compile"
		             && x.Attribute("Include")?.Value == @"..\OtherTestProjects\OtherTestClass.cs"
	        );
			var linkAttribute = linkedFile.Attributes().FirstOrDefault(a => a.Name.LocalName == "Link");
			Assert.IsNotNull(linkAttribute);
			Assert.AreEqual("OtherTestClass.cs", linkAttribute.Value);

	        var sourceWithDesigner = includeItems.Single(
		        x => x.Name.LocalName == "Compile"
		             && x.Attribute("Update")?.Value == @"SourceFileWithDesigner.cs"
	        );

			var subTypeElement = sourceWithDesigner.Elements().Single();
	        Assert.AreEqual("SubType", subTypeElement.Name.LocalName);
	        Assert.AreEqual("Component", subTypeElement.Value);

	        var designerForSource = includeItems.Single(
		        x => x.Name.LocalName == "Compile"
		             && x.Attribute("Update")?.Value == @"SourceFileWithDesigner.Designer.cs"
	        );

	        var dependentUponElement2 = designerForSource.Elements().Single();

	        Assert.AreEqual("DependentUpon", dependentUponElement2.Name.LocalName);
	        Assert.AreEqual("SourceFileWithDesigner.cs", dependentUponElement2.Value);

	        var fileWithAnotherAttribute = includeItems.Single(
		        x => x.Name.LocalName == "Compile"
		             && x.Attribute("Update")?.Value == @"AnotherFile.cs"
	        );

			Assert.AreEqual(2, fileWithAnotherAttribute.Attributes().Count());
			Assert.AreEqual("AttrValue", fileWithAnotherAttribute.Attribute("AnotherAttribute")?.Value);

	        var removeMatchingWildcard = includeItems.Where(
			        x => x.Name.LocalName == "Compile"
			             && x.Attribute("Remove")?.Value != null
		        )
		        .ToImmutableList();
			Assert.IsNotNull(removeMatchingWildcard);
			Assert.AreEqual(2, removeMatchingWildcard.Count);
			Assert.IsTrue(removeMatchingWildcard.Any(x => x.Attribute("Remove")?.Value == "SourceFileAsResource.cs"));
			Assert.IsTrue(removeMatchingWildcard.Any(x => x.Attribute("Remove")?.Value == "Class1.cs"));
        }
    }
}
