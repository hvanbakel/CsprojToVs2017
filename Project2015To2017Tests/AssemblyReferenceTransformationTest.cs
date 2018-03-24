using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using hvanbakel.Project2015To2017;
using hvanbakel.Project2015To2017.Definition;

namespace Project2015To2017Tests
{
    [TestClass]
    public class AssemblyReferenceTransformationTest
    {
        [TestMethod]
        public async Task TransformsAssemblyReferencesAsync()
        {
            var project = new Project();
            var transformation = new AssemblyReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

	        var progress = new Progress<string>(x => { });

            await transformation.TransformAsync(doc, directoryInfo, project, progress).ConfigureAwait(false);

            Assert.AreEqual(11, project.AssemblyReferences.Count);
            Assert.IsTrue(project.AssemblyReferences.Any(x => x.Include == @"System.Xml.Linq"));
            Assert.IsFalse(project.AssemblyReferences.Any(x => x.Include == @"Microsoft.CSharp"));
        }

        [TestMethod]
        public void RemoveExtraAssemblyReferences()
        {
            var project = new Project
            {
                AssemblyReferences = new List<AssemblyReference>
                {
                    new AssemblyReference {Include = "Test.Package", EmbedInteropTypes = "false", HintPath = @"..\packages\Test.Package.dll", Private = "false", SpecificVersion = "false"},
                    new AssemblyReference {Include = "Other.Package", EmbedInteropTypes = "false", HintPath = @"..\packages\Other.Package.dll", Private = "false", SpecificVersion = "false"}
                },
                PackageReferences = new List<PackageReference>
                {
                    new PackageReference {Id = "Test.Package", IsDevelopmentDependency = false, Version = "1.2.3"},
                    new PackageReference {Id = "Another.Package", IsDevelopmentDependency = false, Version = "3.2.1"}
                }
            };

            AssemblyReferenceTransformation.RemoveExtraAssemblyReferences(project);

            Assert.AreEqual(1, project.AssemblyReferences.Count);
            Assert.AreEqual(2, project.PackageReferences.Count);
        }
    }
}
