using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
    public class PackageReferenceTransformationTest
    {
        [TestMethod]
        public void AddsTestPackages()
        {
	        var project = new ProjectReader(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj")).Read();

	        project.Type = ApplicationType.TestProject;
	        project.TargetFrameworks.Add("net45");

            var transformation = new TestProjectPackageReferenceTransformation();

	        var progress = new Progress<string>(x => { });

            transformation.Transform(project, progress);

            Assert.AreEqual(10, project.PackageReferences.Count);
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && x.Version == "15.0.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
        }

        [TestMethod]
        public void AcceptsNetStandardFramework()
        {
	        var project = new ProjectReader(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj")).Read();

	        project.Type = ApplicationType.TestProject;
	        project.TargetFrameworks.Add("netstandard2.0");

			var transformation = new TestProjectPackageReferenceTransformation();

	        var progress = new Progress<string>(x => { });

            transformation.Transform(project, progress);

            Assert.AreEqual(10, project.PackageReferences.Count);
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && x.Version == "15.0.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
        }

        [TestMethod]
        public void DoesNotAddTestPackagesIfExists()
        {
            var transformation = new TestProjectPackageReferenceTransformation();

			var project = new ProjectReader(Path.Combine("TestFiles", "OtherTestProjects", "containsTestSDK.testcsproj")).Read();

	        project.TargetFrameworks.Add("net45");

	        var progress = new Progress<string>(x => { });

            transformation.Transform(project, progress);

            Assert.AreEqual(6, project.PackageReferences.Count);
            Assert.AreEqual(0, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter"));
        }

        [TestMethod]
        public void TransformsPackages()
        {
	        var project = new ProjectReader(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj")).Read();

            var transformation = new TestProjectPackageReferenceTransformation();

	        var progress = new Progress<string>(x => { });

            transformation.Transform(project, progress);

            Assert.AreEqual(7, project.PackageReferences.Count);
            Assert.AreEqual(2, project.PackageReferences.Count(x => x.IsDevelopmentDependency));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin" && x.Version == "3.1.0"));
        }

        [TestMethod]
        public void HandlesNonXml()
        {
            var project = new ProjectReader(Path.Combine("TestFiles", "OtherTestProjects", "net46console.testcsproj")).Read();
            var transformation = new TestProjectPackageReferenceTransformation();

	        var progress = new Progress<string>(x => { });

            transformation.Transform(project, progress);
        }
    }
}
