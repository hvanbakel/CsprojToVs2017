using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Writing;

namespace Project2015To2017Tests
{
	[TestClass]
	public class ProjectWriterTest
	{
		[TestMethod]
		public void GenerateAssemblyInfoOnNothingSpecifiedTest()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				AssemblyAttributes = new AssemblyAttributes()
			}, new FileInfo("test.cs"));

			var generateAssemblyInfo = xmlNode.Element("PropertyGroup").Element("GenerateAssemblyInfo");
			Assert.IsNotNull(generateAssemblyInfo);
			Assert.AreEqual("false", generateAssemblyInfo.Value);
		}

		[TestMethod]
		public async Task WritesDistinctConfigurations()
		{
			const string xml = @"
<Project DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"" ToolsVersion=""4.0"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <VisualStudioVersion Condition=""'$(VisualStudioVersion)' == ''"">15.0</VisualStudioVersion>
    <OldToolsVersion>14.0</OldToolsVersion>
    <DslTargetsPath>..\SDK\v15.0\MSBuild\DSLTools</DslTargetsPath>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
    <MinimumVisualStudioVersion>15.0</MinimumVisualStudioVersion>
  </PropertyGroup>
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{87161453-D71B-4ABB-BADB-1D0E621E8EA0}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Class1</RootNamespace>
    <AssemblyName>Class1</AssemblyName>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <TargetFrameworkProfile />
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|x86' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <DocumentationFile>bin\Debug\Class1.xml</DocumentationFile>
    <RunCodeAnalysis>false</RunCodeAnalysis>
    <CodeAnalysisRuleSet>..\FxCop.Rules.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|x64' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <DocumentationFile>bin\Release\Class1.xml</DocumentationFile>
    <RunCodeAnalysis>true</RunCodeAnalysis>
    <CodeAnalysisRuleSet>..\FxCop.Rules.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x86'"">
    <OutputPath>bin\Release_CI\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <Optimize>true</Optimize>
    <DebugType>pdbonly</DebugType>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>..\FxCop.Rules.ruleset</CodeAnalysisRuleSet>
    <DocumentationFile>bin\Release_CI\Class1.xml</DocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RunCodeAnalysis>true</RunCodeAnalysis>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release|x64'"">
    <OutputPath>bin\Release_CI\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <Optimize>true</Optimize>
    <DebugType>pdbonly</DebugType>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>..\FxCop.Rules.ruleset</CodeAnalysisRuleSet>
    <DocumentationFile>bin\Release_CI\Class1.xml</DocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RunCodeAnalysis>true</RunCodeAnalysis>
  </PropertyGroup>
 </Project>";

			var project = await ParseAndTransform(xml).ConfigureAwait(false);

			Assert.AreEqual(4, project.Configurations.Count);
			Assert.AreEqual(2, project.Configurations.Count(x => x == "Debug"));
			Assert.AreEqual(2, project.Configurations.Count(x => x == "Release"));

			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(project, new FileInfo("test.cs"));

			var generatedConfigurations = xmlNode.Element("PropertyGroup").Element("Configurations");
			Assert.AreEqual("Debug;Release", generatedConfigurations.Value);
		}

		[TestMethod]
		public void GeneratesAssemblyInfoNodesWhenSpecifiedTest()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				AssemblyAttributes = new AssemblyAttributes {Company = "Company"}
			}, new FileInfo("test.cs"));

			var generateAssemblyInfo = xmlNode.Element("PropertyGroup").Element("GenerateAssemblyInfo");
			Assert.IsNull(generateAssemblyInfo);

			var generateAssemblyCompany = xmlNode.Element("PropertyGroup").Element("GenerateAssemblyCompanyAttribute");
			Assert.IsNotNull(generateAssemblyCompany);
			Assert.AreEqual("false", generateAssemblyCompany.Value);
		}

		[TestMethod]
		public void SkipDelaySignNull()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				DelaySign = null
			}, new FileInfo("test.cs"));

			var delaySign = xmlNode.Element("PropertyGroup").Element("DelaySign");
			Assert.IsNull(delaySign);
		}

		[TestMethod]
		public void OutputDelaySignTrue()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				DelaySign = true
			}, new FileInfo("test.cs"));

			var delaySign = xmlNode.Element("PropertyGroup").Element("DelaySign");
			Assert.IsNotNull(delaySign);
			Assert.AreEqual("true", delaySign.Value);
		}

		[TestMethod]
		public void OutputDelaySignFalse()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				DelaySign = false
			}, new FileInfo("test.cs"));

			var delaySign = xmlNode.Element("PropertyGroup").Element("DelaySign");
			Assert.IsNotNull(delaySign);
			Assert.AreEqual("false", delaySign.Value);
		}

		private static async Task<Project> ParseAndTransform(string xml, [CallerMemberName] string memberName = "")
		{
			var testCsProjFile = $"{memberName}_test.csproj";

			await File.WriteAllTextAsync(testCsProjFile, xml);

			var project = new ProjectReader().Read(testCsProjFile);

			return project;
		}
	}
}