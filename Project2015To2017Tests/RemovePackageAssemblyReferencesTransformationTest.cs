using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using System.Collections.Generic;

namespace Project2015To2017Tests
{
    [TestClass]
    public class RemovePackageAssemblyReferencesTransformationTest
    {
        [TestMethod]
        public void HandlesNoPackagesConfig()
        {
            var project = new Project2015To2017.Definition.Project();

            var transformation = new RemovePackageAssemblyReferencesTransformation();
            transformation.TransformAsync(null, null, project);
        }

        [TestMethod]
		public void DedupeReferencesFromPackages()
		{
			var project = new Project2015To2017.Definition.Project
			{
				AssemblyReferences = new List<Project2015To2017.Definition.AssemblyReference>
				{
					new Project2015To2017.Definition.AssemblyReference
					{
						Include = "Newtonsoft.Json, Version=10.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed, processorArchitecture=MSIL",
						HintPath = @"..\packages\Newtonsoft.Json.10.0.2\lib\net45\Newtonsoft.Json.dll"
					},
					new Project2015To2017.Definition.AssemblyReference
					{
						Include = "System.Data.DataSetExtensions"
					},
					new Project2015To2017.Definition.AssemblyReference
					{
						Include = "Owin",
						HintPath = @"..\packages\Owin.1.0\lib\net40\Owin.dll"
					}
				},
				PackageReferences = new[]
				{
					new Project2015To2017.Definition.PackageReference
					{
						Id = "Newtonsoft.Json"
					}
				}
			};

			var transformation = new RemovePackageAssemblyReferencesTransformation();
			transformation.TransformAsync(null, null, project);

			Assert.AreEqual(2, project.AssemblyReferences.Count);
		}
    }
}
