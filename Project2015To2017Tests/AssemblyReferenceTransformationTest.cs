using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class AssemblyReferenceTransformationTest
	{
		[TestMethod]
		public void TransformsAssemblyReferences()
		{
			var project = new ProjectReader().Read("TestFiles\\net46console.testcsproj");
			var transformation = new AssemblyReferenceTransformation();

			var progress = new Progress<string>(x => { });

			var transformedProject = transformation.Transform(project, progress);

			Assert.AreEqual(11, transformedProject.AssemblyReferences.Count);
			Assert.IsTrue(transformedProject.AssemblyReferences.Any(x => x.Include == @"System.Xml.Linq"));
			Assert.IsFalse(transformedProject.AssemblyReferences.Any(x => x.Include == @"Microsoft.CSharp"));
		}

		[TestMethod]
		public void RemoveExtraAssemblyReferences()
		{
			var project = new ProjectBuilder()
			{
				AssemblyReferences = new List<AssemblyReference>
				{
					new AssemblyReference
					(
						include : "Test.Package",
						embedInteropTypes : "false",
						hintPath : @"..\packages\Test.Package.dll",
						isPrivate : "false",
						specificVersion : "false"
					)
					,
					new AssemblyReference
					(
						include : "Other.Package",
						embedInteropTypes : "false",
						hintPath : @"..\packages\Other.Package.dll",
						isPrivate : "false",
						specificVersion : "false"
					)
				},
				PackageReferences = new List<PackageReference>
				{
					new PackageReference
					(
						id : "Test.Package",
						isDevelopmentDependency : false,
						version : "1.2.3"
					)
					,
					new PackageReference
					(
						id : "Another.Package",
						isDevelopmentDependency : false,
						version : "3.2.1"
					)
				}
			}.ToImmutable();

			var transformation = new AssemblyReferenceTransformation();

			var progress = new Progress<string>(x => { });

			var transformedProject = transformation.Transform(project, progress);

			Assert.AreEqual(1, transformedProject.AssemblyReferences.Count);
			Assert.AreEqual(2, transformedProject.PackageReferences.Count);
		}
	}
}
