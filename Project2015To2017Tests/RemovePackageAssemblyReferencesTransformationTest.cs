using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class RemovePackageAssemblyReferencesTransformationTest
	{
		[TestMethod]
		public void HandlesNoPackagesConfig()
		{
			var project = new ProjectBuilder().ToImmutable();


			var progress = new Progress<string>(x => { });

			var transformation = new RemovePackageAssemblyReferencesTransformation();
			transformation.Transform(project, progress);
		}

		[TestMethod]
		public void DedupeReferencesFromPackages()
		{
			var project = new ProjectBuilder
			{
				AssemblyReferences = new List<AssemblyReference>
				{
					new AssemblyReference
						(
						include : "Newtonsoft.Json, Version=10.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL",
						hintPath : @"..\packages\Newtonsoft.Json.10.0.2\lib\net45\Newtonsoft.Json.dll"
					),
					new AssemblyReference
					(
						include : "System.Data.DataSetExtensions"
					),
					new AssemblyReference
					(
						include : "Owin",
						hintPath : @"..\packages\Owin.1.0\lib\net40\Owin.dll"
					)
				},
				PackageReferences = new[]
				{
					new PackageReference
					(
						id : "Newtonsoft.Json",
						version: "1.0.0"
					)
				},
				FilePath = new FileInfo(@".\dummy.csproj")
			}.ToImmutable();

			var transformation = new RemovePackageAssemblyReferencesTransformation();

			var progress = new Progress<string>(x => { });

			var transformedProj = transformation.Transform(project, progress);

			Assert.AreEqual(2, transformedProj.AssemblyReferences.Count);
		}

		[TestMethod]
		public void DedupeReferencesFromPackagesAlternativePackagesFolder()
		{
			var projFile = @"TestFiles\AltNugetConfig\ProjectFolder\net46console.testcsproj";

			var project = new ProjectReader().Read(projFile);

			var transformation = new RemovePackageAssemblyReferencesTransformation();

			var progress = new Progress<string>(x => { });

			//Then attempt to clear any referencing the nuget packages folder
			var adjustedProject = transformation.Transform(project, progress);

			//The only one left which points to another folder
			Assert.AreEqual(1, adjustedProject.AssemblyReferences.Count);
			Assert.IsTrue(adjustedProject.AssemblyReferences[0].Include.StartsWith("Owin"));
		}
	}
}
