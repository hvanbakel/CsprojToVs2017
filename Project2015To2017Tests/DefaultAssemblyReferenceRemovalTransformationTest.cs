using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class DefaultAssemblyReferenceRemovalTransformationTest
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

			new DefaultAssemblyReferenceRemovalTransformation().Transform(project, NoopLogger.Instance);

			Assert.AreEqual(0, project.AssemblyReferences.Count);
		}
	}
}