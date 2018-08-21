using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class XamlTransformationTest
	{
		[TestMethod]
		public async Task TransformsPresentationPages()
		{
			var project = await ParseAndTransform(@"
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
  <PropertyGroup>
    <ApplicationIcon>Dopamine.ico</ApplicationIcon>
  </PropertyGroup>
  <PropertyGroup>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Net.Http"" />
    <Reference Include=""System.Xaml"">
      <RequiredTargetFramework>4.0</RequiredTargetFramework>
    </Reference>
    <Reference Include=""PresentationCore"" />
    <Reference Include=""PresentationFramework"" />
  </ItemGroup>
  <ItemGroup>
    <ApplicationDefinition Include=""App.xaml"">
      <Generator>MSBuild:Compile</Generator>
      <SubType>Designer</SubType>
    </ApplicationDefinition>
    <Compile Include=""App.xaml.cs"">
      <DependentUpon>App.xaml</DependentUpon>
      <SubType>Code</SubType>
    </Compile>
    <Compile Include=""Views\Shell.xaml.cs"">
      <DependentUpon>Shell.xaml</DependentUpon>
    </Compile>
    <Compile Include="".\..\Views\Initialize.xaml.cs"">
      <DependentUpon>Initialize.xaml</DependentUpon>
    </Compile>
  </ItemGroup>
  <ItemGroup>
    <Page Include=""Views\Shell.xaml"">
      <SubType>Designer</SubType>
      <Generator>MSBuild:Compile</Generator>
    </Page>
    <Page Include="".\..\Views\Initialize.xaml"">
      <SubType>Designer</SubType>
      <Generator>MSBuild:Compile</Generator>
    </Page>
  </ItemGroup>
</Project>");

			Assert.IsTrue(project.IsWindowsPresentationFoundationProject);
			Assert.IsFalse(project.IsWindowsFormsProject);

			Assert.AreEqual(1, project.TargetFrameworks.Count);
			Assert.AreEqual(1, project.TargetFrameworks.Count(x => x == "net461"));

			Assert.AreEqual(2, project.Configurations.Count);
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Debug"));
			Assert.AreEqual(1, project.Configurations.Count(x => x == "Release"));

			Assert.AreEqual(1, project.Platforms.Count);
			Assert.AreEqual(1, project.Platforms.Count(x => x == "AnyCPU"));

			Assert.AreEqual(3, project.AdditionalPropertyGroups.Count);

			var fileTransformation = new FileTransformation();
			var transformation = new XamlPagesTransformation();

			var progress = new Progress<string>();

			fileTransformation.Transform(project, progress);
			transformation.Transform(project, progress);

			var includeItems = project.IncludeItems;

			// App.xaml is NOT included due to ApplicationDefinition
			// App.xaml.cs is NOT included due to <SubType>Code</SubType> (FileTransformation)
			// Views\Shell.xaml.cs is NOT included due to Compile+DependentUpon
			// .\..\Views\Initialize.xaml.cs is included due to not in project folder
			// Views\Shell.xaml is NOT included due to Page
			// .\..\Views\Initialize.xaml is included due to Page not in project folder

			Assert.AreEqual(2, includeItems.Count);

			Assert.AreEqual(1, includeItems.Count(x => x.Name == project.XmlNamespace + "Page"));
			Assert.AreEqual(0, includeItems.Count(x => x.Name == project.XmlNamespace + "ApplicationDefinition"));
			Assert.AreEqual(1, includeItems.Count(x => x.Name == project.XmlNamespace + "Compile"));
			Assert.AreEqual(1,
				includeItems.Count(x => x.Name == project.XmlNamespace + "Compile" && x.Attribute("Update") != null));
			Assert.AreEqual(0,
				includeItems.Count(x => x.Name == project.XmlNamespace + "Compile" && x.Attribute("Include") != null));
			Assert.AreEqual(0,
				includeItems.Count(x => x.Name == project.XmlNamespace + "Compile" && x.Attribute("Remove") != null));
		}

		private static async Task<Project> ParseAndTransform(
			string xml,
			[System.Runtime.CompilerServices.CallerMemberName]
			string memberName = ""
		)
		{
			var testCsProjFile = $"{memberName}_test.csproj";

			await File.WriteAllTextAsync(testCsProjFile, xml);

			var project = new ProjectReader(testCsProjFile).Read();

			return project;
		}
	}
}