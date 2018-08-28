using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class AssemblyFilterDefaultTransformationTest
	{
		[TestMethod]
		public void PreventEmptyAssemblyReferences()
		{
			var project = new Project
			{
				AssemblyReferences = new List<AssemblyReference>
				{
					new AssemblyReference
					{
						Include = "System"
					}
				},
				FilePath = new FileInfo("test.cs")
			};

			new AssemblyFilterDefaultTransformation().Transform(project);

			Assert.AreEqual(0, project.AssemblyReferences.Count);
		}
	}
}