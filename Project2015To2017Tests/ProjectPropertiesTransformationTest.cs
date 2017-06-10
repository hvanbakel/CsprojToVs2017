using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using Project2015To2017;
using System.Threading.Tasks;
using Project2015To2017.Definition;
using System;

namespace Project2015To2017Tests
{
    [TestClass]
    public class ProjectPropertiesTransformationTest
    {
        [TestMethod]
        public async Task ReadsTestProject()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
    <TestProjectType>UnitTest</TestProjectType>
  </PropertyGroup>
 <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(ApplicationType.TestProject, project.Type);
            Assert.AreEqual("net46", project.TargetFrameworks[0]);
        }

        [TestMethod]
        public async Task ReadsConsoleApplication()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(ApplicationType.ConsoleApplication, project.Type);
            Assert.AreEqual("net46", project.TargetFrameworks[0]);
        }

        [TestMethod]
        public async Task ReadsClassLibraryApplication()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(ApplicationType.ClassLibrary, project.Type);
            Assert.AreEqual("net462", project.TargetFrameworks[0]);
        }

        [TestMethod]
        public async Task ReadsRootNamespace()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <RootNamespace>MyProject</RootNamespace>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual("MyProject", project.RootNamespace);
        }

        [TestMethod]
        public async Task ReadsAssemblyName()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <AssemblyName>MyProject</AssemblyName>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual("MyProject", project.AssemblyName);
        }

        [TestMethod]
        public async Task ReadsOptimize()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <Optimize>true</Optimize>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(true, project.Optimize);
        }

        [TestMethod]
        public async Task ReadsTreatWarningsAsErrors()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(true, project.TreatWarningsAsErrors);
        }

        [TestMethod]
        public async Task ReadsAllowUnsafeBlocks()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(true, project.AllowUnsafeBlocks);
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public async Task ThrowsOnNoUnconditionalPropertyGroup()
		{
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
	<OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <DefineConstants>foo</DefineConstants>
  </PropertyGroup>
</Project>";

			await ParseAndTransformAsync(xml).ConfigureAwait(false);
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public async Task ThrowsOnNoOutput()
		{
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <DefineConstants>foo</DefineConstants>
  </PropertyGroup>
</Project>";

			await ParseAndTransformAsync(xml).ConfigureAwait(false);
		}

		[TestMethod]
        public async Task ReadsWindowsApplication()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Winexe</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(ApplicationType.WindowsApplication, project.Type);
            Assert.AreEqual("net462", project.TargetFrameworks[0]);
        }

        private static async Task<Project> ParseAndTransformAsync(string xml)
        {
            var document = XDocument.Parse(xml);

            var transformation = new ProjectPropertiesTransformation();

            var project = new Project();
            await transformation.TransformAsync(document, null, project).ConfigureAwait(false);
            return project;
        }
    }
}
