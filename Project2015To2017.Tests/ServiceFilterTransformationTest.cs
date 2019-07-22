using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017.Tests
{
	[TestClass]
	public class ServiceFilterTransformationTest
	{
		[DataRow(0, 0)]
		[DataRow(15, 0)]
		[DataRow(15, 6)]
		[DataRow(15, 7)]
		[DataRow(16, 0)]
		[TestMethod]
		public void RemovesServiceTagWithKnownTestFramework(int visualStudioVersionMajor, int visualStudioVersionMinor)
		{
			var guid = Guid.NewGuid();
			var xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{" + guid.ToString() + @"}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ClassLibrary1</RootNamespace>
    <AssemblyName>ClassLibrary1</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <SccProjectName>SAK</SccProjectName>
    <SccLocalPath>SAK</SccLocalPath>
    <SccAuxPath>SAK</SccAuxPath>
    <SccProvider>SAK</SccProvider>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">10.0</VisualStudioVersion>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />
    <PackageReference Include=""NUnit"" />
  </ItemGroup>
 </Project>";

			var project = ParseAndTransform(xml, projectName: "Class1");
			var name = "someproject";
			project.ProjectName = name;
			project.Solution = new Solution
			{
				ProjectPaths = new[]
				{
					new ProjectReference
					{
						ProjectName = name,
						ProjectGuid = guid
					}
				}
			};

			new ServiceFilterTransformation(new Version(visualStudioVersionMajor, visualStudioVersionMinor)).Transform(project);

			Assert.IsFalse(project.ProjectDocument.Descendants().Any(x => x.Name.LocalName == "Service"));
		}

		[DataRow(0, 0, true)]
		[DataRow(15, 0, true)]
		[DataRow(15, 6, true)]
		[DataRow(15, 7, false)]
		[DataRow(16, 0, false)]
		[TestMethod]
		public void RemovesServiceTagWithVisualStudioVersion(int visualStudioVersionMajor, int visualStudioVersionMinor, bool expected)
		{
			var guid = Guid.NewGuid();
			var xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{" + guid.ToString() + @"}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ClassLibrary1</RootNamespace>
    <AssemblyName>ClassLibrary1</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <SccProjectName>SAK</SccProjectName>
    <SccLocalPath>SAK</SccLocalPath>
    <SccAuxPath>SAK</SccAuxPath>
    <SccProvider>SAK</SccProvider>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">10.0</VisualStudioVersion>
  </PropertyGroup>
  <ItemGroup>
    <Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />
  </ItemGroup>
 </Project>";

			var project = ParseAndTransform(xml, projectName: "Class1");
			var name = "someproject";
			project.ProjectName = name;
			project.Solution = new Solution
			{
				ProjectPaths = new[]
				{
					new ProjectReference
					{
						ProjectName = name,
						ProjectGuid = guid
					}
				}
			};

			new ServiceFilterTransformation(new Version(visualStudioVersionMajor, visualStudioVersionMinor)).Transform(project);

			Assert.AreEqual(expected, project.ProjectDocument.Descendants().Any(x => x.Name.LocalName == "Service"));
		}

		[DataRow(0, 0)]
		[DataRow(15, 0)]
		[DataRow(15, 6)]
		[DataRow(15, 7)]
		[DataRow(16, 0)]
		[TestMethod]
		public void DoesNotRemoveServiceTag(int visualStudioVersionMajor, int visualStudioVersionMinor)
		{
			var guid = Guid.NewGuid();
			var xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{" + guid.ToString() + @"}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ClassLibrary1</RootNamespace>
    <AssemblyName>ClassLibrary1</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <SccProjectName>SAK</SccProjectName>
    <SccLocalPath>SAK</SccLocalPath>
    <SccAuxPath>SAK</SccAuxPath>
    <SccProvider>SAK</SccProvider>
	<VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">10.0</VisualStudioVersion>
	<Service Include=""{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}"" />
  </PropertyGroup>
 </Project>";

			var project = ParseAndTransform(xml, projectName: "Class1");
			var name = "someproject";
			project.ProjectName = name;
			project.Solution = new Solution
			{
				ProjectPaths = new[]
				{
					new ProjectReference
					{
						ProjectName = name,
						ProjectGuid = guid
					}
				}
			};

			new ServiceFilterTransformation(new Version(visualStudioVersionMajor, visualStudioVersionMinor)).Transform(project);

			Assert.IsTrue(project.ProjectDocument.Descendants().Any(x => x.Name.LocalName == "Service"));
		}

		private static Project ParseAndTransform(
			string xml,
			[System.Runtime.CompilerServices.CallerMemberName]
			string memberName = "",
			string projectName = null
		)
		{
			var testCsProjFile = $"{memberName}_test.csproj";

			File.WriteAllText(testCsProjFile, xml, Encoding.UTF8);

			var project = new ProjectReader().Read(testCsProjFile);
			project.ProjectName = projectName;
			project.FilePath = null;

			return project;
		}
	}
}