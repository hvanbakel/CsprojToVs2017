using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017Tests
{
    [TestClass]
    public class PackageReferenceTransformationTest
    {
        [TestMethod]
        public async Task AddsTestPackagesAsync()
        {
            var project = new Project { Type = ApplicationType.TestProject, TargetFrameworks = new[] { "net45" } };
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(10, project.PackageReferences.Count);
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && x.Version == "15.0.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
        }

        [TestMethod]
        public async Task AcceptsNetStandardFrameworkAsync()
        {
            var project = new Project { Type = ApplicationType.TestProject, TargetFrameworks = new[] { "netstandard2.0" } };
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(10, project.PackageReferences.Count);
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin.Host.HttpListener" && x.Version == "3.1.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.NET.Test.Sdk" && x.Version == "15.0.0"));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "AutoMapper" && x.Version == "6.1.1" && x.IsDevelopmentDependency));
        }

        [TestMethod]
        public async Task DoesNotAddTestPackagesIfExistsAsync()
        {
            var project = new Project { Type = ApplicationType.TestProject, TargetFrameworks = new[] { "net45" } };
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Parse(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""15.0.0"" />
  </ItemGroup>
</Project>
");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(6, project.PackageReferences.Count);
            Assert.AreEqual(0, project.PackageReferences.Count(x => x.Id == "MSTest.TestAdapter"));
        }

        [TestMethod]
        public async Task TransformsPackagesAsync()
        {
            var project = new Project();
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\TestFiles");
            var doc = XDocument.Load("TestFiles\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);

            Assert.AreEqual(7, project.PackageReferences.Count);
            Assert.AreEqual(2, project.PackageReferences.Count(x => x.IsDevelopmentDependency));
            Assert.AreEqual(1, project.PackageReferences.Count(x => x.Id == "Microsoft.Owin" && x.Version == "3.1.0"));
        }

        [TestMethod]
        public async Task HandlesNonXmlAsync()
        {
            var project = new Project();
            var transformation = new PackageReferenceTransformation();

            var directoryInfo = new DirectoryInfo(".\\OtherPackagesConfig");
            var doc = XDocument.Load("OtherPackagesConfig\\net46console.testcsproj");

            await transformation.TransformAsync(doc, directoryInfo, project).ConfigureAwait(false);
        }
    }
}
