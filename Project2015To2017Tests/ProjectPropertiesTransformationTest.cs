using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using System.Threading.Tasks;
using System;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017;

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
        public async Task ReadsTestProjectGuid()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <ProjectTypeGuids>{3AC096D0-A1C2-E12C-1390-A8335801FDAB};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
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
            Assert.AreEqual("net40", project.TargetFrameworks[0]);
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
        public async Task ReadsConsoleApplicationFromConditional()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>
  </PropertyGroup>  
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugType>Full</DebugType>
    <Optimize>False</Optimize>
    <OutputType>Exe</OutputType>
    <DebugSymbols>true</DebugSymbols>
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
		public async Task ReadsSigningProperties()
		{
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>Project.snk</AssemblyOriginatorKeyFile>
    <DelaySign>true</DelaySign>
  </PropertyGroup>
</Project>";

			var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

			Assert.AreEqual(true, project.SignAssembly);
			Assert.AreEqual("Project.snk", project.AssemblyOriginatorKeyFile);
			Assert.AreEqual(true, project.DelaySign);
		}

		[TestMethod]
		public async Task ReadsDefaultSigningProperties()
		{
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";

			var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

			Assert.AreEqual(false, project.SignAssembly);
			Assert.AreEqual(null, project.AssemblyOriginatorKeyFile);
			Assert.AreEqual(null, project.DelaySign);
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

		[TestMethod]
		public async Task ReadsIOSApplication()
		{
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""..\..\packages\Xamarin.Forms.2.4.0.38779\build\netstandard1.0\Xamarin.Forms.props"" Condition=""Exists('..\..\packages\Xamarin.Forms.2.4.0.38779\build\netstandard1.0\Xamarin.Forms.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">iPhoneSimulator</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <OutputType>Exe</OutputType>
    <RootNamespace>App.iOS</RootNamespace>
    <IPhoneResourcePrefix>Resources</IPhoneResourcePrefix>
    <AssemblyName>App.iOS</AssemblyName>
    <NuGetPackageImportStamp>
    </NuGetPackageImportStamp>
  </PropertyGroup>
</Project>";

			var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

			Assert.IsNull(project.TargetFrameworks);
        }

        [TestMethod]
        public async Task ReadsPropertiesWithMultipleUnconditionalPropertyGroups()
        {
            var xml = @"
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
    <ProductVersion>9.0.30729</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{9F9AF5F0-C2CF-48B9-BF38-FEC89FDABA4A}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Croc.XFW3.DomainModelDefinitionLanguage</RootNamespace>
    <AssemblyName>Croc.XFW3.DomainModelDefinitionLanguage.Dsl</AssemblyName>
    <SolutionDir Condition=""$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'"">..\</SolutionDir>
    <RestorePackages>true</RestorePackages>
    <IncludeDebugSymbolsInVSIXContainer>true</IncludeDebugSymbolsInVSIXContainer>
    <RestoreProjectStyle>PackageReference</RestoreProjectStyle>
    <RuntimeIdentifier>win7-x86</RuntimeIdentifier>
  </PropertyGroup>
</Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual("Croc.XFW3.DomainModelDefinitionLanguage.Dsl", project.AssemblyName);
            Assert.AreEqual("Croc.XFW3.DomainModelDefinitionLanguage", project.RootNamespace);
            Assert.AreEqual(ApplicationType.ClassLibrary, project.Type);
        }

        [TestMethod]
        public async Task ReadsImportsAndTargets()
        {
            var xml = @"
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
    <ProductVersion>9.0.30729</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{9F9AF5F0-C2CF-48B9-BF38-FEC89FDABA4A}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Croc.XFW3.DomainModelDefinitionLanguage</RootNamespace>
    <AssemblyName>Croc.XFW3.DomainModelDefinitionLanguage.Dsl</AssemblyName>
    <SolutionDir Condition=""$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'"">..\</SolutionDir>
    <RestorePackages>true</RestorePackages>
    <IncludeDebugSymbolsInVSIXContainer>true</IncludeDebugSymbolsInVSIXContainer>
    <RestoreProjectStyle>PackageReference</RestoreProjectStyle>
    <RuntimeIdentifier>win7-x86</RuntimeIdentifier>
  </PropertyGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <Import Project=""Other"" />
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild""> 
  </Target>
 </Project>";

            var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

            Assert.AreEqual(1, project.Imports.Count);
            Assert.AreEqual(2, project.Targets.Count);
		}

		[TestMethod]
		public async Task CopiesTFVCPropertyGroup()
		{
			var xml = @"
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
    <ProjectGuid>{D8141286-2A5C-4CC4-8502-8E651D35F371}</ProjectGuid>
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
  </PropertyGroup>
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>9.0.30729</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{9F9AF5F0-C2CF-48B9-BF38-FEC89FDABA4A}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Croc.XFW3.DomainModelDefinitionLanguage</RootNamespace>
    <AssemblyName>Croc.XFW3.DomainModelDefinitionLanguage.Dsl</AssemblyName>
    <SolutionDir Condition=""$(SolutionDir) == '' Or $(SolutionDir) == '*Undefined*'"">..\</SolutionDir>
    <RestorePackages>true</RestorePackages>
    <IncludeDebugSymbolsInVSIXContainer>true</IncludeDebugSymbolsInVSIXContainer>
    <RestoreProjectStyle>PackageReference</RestoreProjectStyle>
    <RuntimeIdentifier>win7-x86</RuntimeIdentifier>
  </PropertyGroup>
 </Project>";

			var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

			Assert.AreEqual(1, project.AdditionalPropertyGroups.Count);
			Assert.AreEqual(4, project.AdditionalPropertyGroups[0].Elements().Count());
		}

		[TestMethod]
		public async Task MaintainsPrePostBuildEvent()
		{
			var xml = @"
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
    <ProjectGuid>{D8141286-2A5C-4CC4-8502-8E651D35F371}</ProjectGuid>
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
  </PropertyGroup>
  <PropertyGroup>
    <PostBuildEvent>CD $(SolutionDir)

if $(ConfigurationName) == Debug (
	xcopy "".\Configuration\Log4Net\*.config""  ""$(TargetDir)"" /E /Y
)</PostBuildEvent>
  </PropertyGroup>
 </Project>";

			var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

			Assert.AreEqual(1, project.BuildEvents.Count);
		}

		[TestMethod]
		public async Task ReadsConfigurations()
		{
			var xml = @"
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
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
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
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
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
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)' == 'Release_CI|AnyCPU'"">
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

			var project = await ParseAndTransformAsync(xml).ConfigureAwait(false);

			Assert.AreEqual(3, project.Configurations.Count);
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Debug"));
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Release_CI"));
		}

		private static async Task<Project> ParseAndTransformAsync(string xml)
		{
			var document = XDocument.Parse(xml);

			var transformation = new ProjectPropertiesTransformation();
			
			var progress = new Progress<string>(x => { });

			var project = new Project();
			await transformation.TransformAsync(document, null, project, progress).ConfigureAwait(false);
			return project;
		}
	}
}