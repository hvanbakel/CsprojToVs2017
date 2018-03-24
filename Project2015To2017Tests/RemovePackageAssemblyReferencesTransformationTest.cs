using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using hvanbakel.Project2015To2017;
using hvanbakel.Project2015To2017.Definition;

namespace Project2015To2017Tests
{
    [TestClass]
    public class RemovePackageAssemblyReferencesTransformationTest
    {
        [TestMethod]
        public void HandlesNoPackagesConfig()
        {
            var project = new Project();

			
	        var progress = new Progress<string>(x => { });

            var transformation = new RemovePackageAssemblyReferencesTransformation();
            transformation.TransformAsync(null, null, project, progress);
        }

        [TestMethod]
		public void DedupeReferencesFromPackages()
		{
			var project = new Project
			{
				AssemblyReferences = new List<AssemblyReference>
				{
					new AssemblyReference
					{
						Include = "Newtonsoft.Json, Version=10.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL",
						HintPath = @"..\packages\Newtonsoft.Json.10.0.2\lib\net45\Newtonsoft.Json.dll"
					},
					new AssemblyReference
					{
						Include = "System.Data.DataSetExtensions"
					},
					new AssemblyReference
					{
						Include = "Owin",
						HintPath = @"..\packages\Owin.1.0\lib\net40\Owin.dll"
					}
				},
				PackageReferences = new[]
				{
					new PackageReference
					{
						Id = "Newtonsoft.Json"
					}
				}
			};

			var transformation = new RemovePackageAssemblyReferencesTransformation();

			var projectFolder = new DirectoryInfo(".");
			
			var progress = new Progress<string>(x => { });

			transformation.TransformAsync(null, projectFolder, project, progress);

			Assert.AreEqual(2, project.AssemblyReferences.Count);
		}

	    [TestMethod]
	    public async Task DedupeReferencesFromPackagesAlternativePackagesFolderAsync()
	    {
		    var project = new Project();
		    var transformation = new RemovePackageAssemblyReferencesTransformation();
		    
		    var projFile = @"TestFiles\AltNugetConfig\ProjectFolder\net46console.testcsproj";
		    var projFolder = new FileInfo(projFile).Directory;

		    var doc = XDocument.Load(projFile);

			
		    var progress = new Progress<string>(x => { });

			//First load information about the references and package references
		    await new AssemblyReferenceTransformation().TransformAsync(doc, projFolder, project, progress).ConfigureAwait(false);
			await new PackageReferenceTransformation().TransformAsync(doc, projFolder, project, progress).ConfigureAwait(false);

			//Then attempt to clear any referencing the nuget packages folder
		    await transformation.TransformAsync(doc, projFolder, project, progress).ConfigureAwait(false);

			//The only one left which points to another folder
		    Assert.AreEqual(1, project.AssemblyReferences.Count);
		    Assert.IsTrue(project.AssemblyReferences[0].Include.StartsWith("Owin"));
	    }
    }
}
