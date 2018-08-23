using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Project2015To2017.Definition;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using static Project2015To2017.Extensions;
using Project2015To2017;

namespace Project2015To2017Tests
{
	[TestClass]
	public class PropertySimplificationTransformationTest
	{
		[TestMethod]
		public void SimplifiesProperties1()
		{
			const string xml = @"
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{5C9DE16E-C69A-4182-9C0C-30FF7CC944CD}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Dopamine.Tests</RootNamespace>
    <AssemblyName>Dopamine.Tests</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <ProjectTypeGuids>{3AC096D0-A1C2-E12C-1390-A8335801FDAB};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">10.0</VisualStudioVersion>
    <VSToolsPath Condition=""'$(VSToolsPath)' == ''"">$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)</VSToolsPath>
    <ReferencePath>$(ProgramFiles)\Common Files\microsoft shared\VSTT\$(VisualStudioVersion)\UITestExtensionPackages</ReferencePath>
    <IsCodedUITest>False</IsCodedUITest>
    <TestProjectType>UnitTest</TestProjectType>
    <SccProjectName>
    </SccProjectName>
    <SccLocalPath>
    </SccLocalPath>
    <SccAuxPath>
    </SccAuxPath>
    <SccProvider>
    </SccProvider>
    <NuGetPackageImportStamp>
    </NuGetPackageImportStamp>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <Version>1.0.0</Version>
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

			var project = ParseAndTransform(xml, projectName: "Dopamine.Tests");

			Assert.AreEqual(3, project.PropertyGroups.Count);

			Assert.IsNull(project.PropertyGroups[0].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[1].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[2].Attribute("Condition"));

			var childrenGlobal = project.PrimaryPropertyGroup().Elements().ToImmutableArray();
			Assert.AreEqual(8, childrenGlobal.Length);
			Assert.IsTrue(ValidateChildren(childrenGlobal,
				"ProjectGuid", "ProjectTypeGuids", "VisualStudioVersion", "VSToolsPath",
				"ReferencePath", "IsCodedUITest", "TestProjectType", "TargetFrameworkVersion"));

			var childrenDebug = project.PropertyGroups[1].Elements().ToImmutableArray();
			Assert.AreEqual(2, childrenDebug.Length);
			Assert.IsTrue(ValidateChildren(childrenDebug, "DebugType", "OutputPath"));
			var childrenRelease = project.PropertyGroups[2].Elements().ToImmutableArray();
			Assert.AreEqual(2, childrenRelease.Length);
			Assert.IsTrue(ValidateChildren(childrenRelease, "DebugType", "OutputPath"));
		}

		[TestMethod]
		public void SimplifiesProperties2()
		{
			const string xml = @"
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <AssemblyName>Dopamine</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <ProjectTypeGuids>{60dc8134-eba5-43b8-bcc9-bb4bc16c2548};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DefineConstants>TRACE;DEBUG;WINDOWS_DESKTOP</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>
</Project>";

			var project = ParseAndTransform(xml, projectName: "Dopamine");

			Assert.IsTrue(project.IsWindowsPresentationFoundationProject);
			Assert.IsFalse(project.IsWindowsFormsProject);

			Assert.AreEqual(1, project.TargetFrameworks.Count);
			Assert.AreEqual(1, project.TargetFrameworks.Count(x => x == "net461"));

			Assert.AreEqual(2, project.Configurations.Count);
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Debug"));
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Release"));

			Assert.AreEqual(1, project.Platforms.Count);
			Assert.AreEqual(1, project.Platforms.Count(x => x == "AnyCPU"));

			Assert.AreEqual(3, project.PropertyGroups.Count);

			Assert.IsNull(project.PropertyGroups[0].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[1].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[2].Attribute("Condition"));


			var childrenGlobal = project.PrimaryPropertyGroup().Elements().ToImmutableArray();
			Assert.AreEqual(3, childrenGlobal.Length);
			Assert.IsTrue(ValidateChildren(childrenGlobal,
				"OutputType", "TargetFrameworkVersion", "ProjectTypeGuids"));

			var childrenDebug = project.PropertyGroups[1].Elements().ToImmutableArray();
			Assert.AreEqual(1, childrenDebug.Length);
			// non-standard additional WINDOWS_DESKTOP constant present only in Debug
			Assert.IsTrue(ValidateChildren(childrenDebug, "DefineConstants"));

			var childrenRelease = project.PropertyGroups[2].Elements().ToImmutableArray();
			Assert.AreEqual(0, childrenRelease.Length);
		}


		[TestMethod]
		public void HandlesComplexConditions()
		{
			const string xml = @"
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <AssemblyName>Dopamine.Tests</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)' == 'Debug' And '$(Platform)' == 'AnyCPU' "">
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Platform)|$(Configuration)' == 'AnyCPU|Release' "">
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>
</Project>";

			var project = ParseAndTransform(xml, projectName: "Dopamine.Tests");

			Assert.AreEqual(2, project.Configurations.Count);
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Debug"));
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Release"));

			Assert.AreEqual(1, project.Platforms.Count);
			Assert.AreEqual(1, project.Platforms.Count(x => x == "AnyCPU"));

			Assert.AreEqual(3, project.PropertyGroups.Count);

			Assert.IsNull(project.PropertyGroups[0].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[1].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[2].Attribute("Condition"));

			var childrenGlobal = project.PrimaryPropertyGroup().Elements().ToImmutableArray();
			Assert.AreEqual(1, childrenGlobal.Length);
			Assert.IsTrue(ValidateChildren(childrenGlobal, "TargetFrameworkVersion"));

			var childrenDebug = project.PropertyGroups[1].Elements().ToImmutableArray();
			Assert.AreEqual(2, childrenDebug.Length);
			Assert.IsTrue(ValidateChildren(childrenDebug, "DebugType", "OutputPath"));

			var childrenRelease = project.PropertyGroups[2].Elements().ToImmutableArray();
			Assert.AreEqual(1, childrenRelease.Length);
			Assert.IsTrue(ValidateChildren(childrenRelease, "OutputPath"));
		}

		[TestMethod]
		public void HandlesUnknownConfigurationSimplifications()
		{
			const string xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <Configurations>Debug;Release</Configurations>
  </PropertyGroup>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <AssemblyName>Class1</AssemblyName>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <Optimize>false</Optimize>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <Optimize>true</Optimize>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release_CI|AnyCPU'"">
    <OutputPath>bin/Release_CI\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <Optimize>true</Optimize>
    <CodeAnalysisRuleSet>..\FxCop.Rules.ruleset</CodeAnalysisRuleSet>
    <DocumentationFile>bin\Release_CI\Class1.xml</DocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RunCodeAnalysis>true</RunCodeAnalysis>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
 </Project>";

			var project = ParseAndTransform(xml, projectName: "Class1");

			// Configurations property must take precedence
			// Release_CI will be ignored, but still some transformations will apply
			// We must assume if user intentionally omits things from Configurations or Platforms
			// they did that in full awareness of the consequences
			Assert.AreEqual(2, project.Configurations.Count);
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Debug"));
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Release"));

			Assert.AreEqual(5, project.PropertyGroups.Count);

			Assert.IsNull(project.PropertyGroups[0].Attribute("Condition"));
			Assert.IsNull(project.PropertyGroups[1].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[2].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[3].Attribute("Condition"));
			Assert.IsNotNull(project.PropertyGroups[4].Attribute("Condition"));

			var childrenGlobal = project.UnconditionalGroups().Elements().ToImmutableArray();
			Assert.AreEqual(2, childrenGlobal.Length);
			Assert.IsTrue(ValidateChildren(childrenGlobal, "TargetFrameworkVersion"));

			var childrenDebug = project.PropertyGroups[2].Elements().ToImmutableArray();
			Assert.AreEqual(0, childrenDebug.Length);

			var childrenRelease = project.PropertyGroups[3].Elements().ToImmutableArray();
			Assert.AreEqual(0, childrenRelease.Length);

			var childrenReleaseCI = project.PropertyGroups[4].Elements().ToImmutableArray();
			// We remove only one property set to global default (FileAlignment)
			Assert.AreEqual(7, childrenReleaseCI.Length);
			Assert.IsTrue(ValidateChildren(childrenReleaseCI,
				"DefineConstants", "OutputPath", "Optimize", "CodeAnalysisRuleSet", "DocumentationFile",
				"TreatWarningsAsErrors", "RunCodeAnalysis"));
			// check we are keeping original slashes and replacing configuration name with $(Configuration)
			Assert.AreEqual(@"bin/$(Configuration)\",
				childrenReleaseCI.First(x => x.Name.LocalName == "OutputPath").Value);
		}

		private static Project ParseAndTransform(
			string xml,
			[System.Runtime.CompilerServices.CallerMemberName]
			string memberName = "",
			string projectName = null
		)
		{
			var testCsProjFile = $"{memberName}_test.csproj";

			File.WriteAllText(testCsProjFile, xml);

			var project = new ProjectReader().Read(testCsProjFile);
			project.ProjectName = projectName;
			project.FilePath = null;

			new PropertySimplificationTransformation().Transform(project);

			return project;
		}
	}
}