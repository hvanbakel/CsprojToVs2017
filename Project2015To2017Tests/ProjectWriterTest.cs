using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;
using Project2015To2017.Reading;
using Project2015To2017.Writing;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Project2015To2017Tests
{
	[TestClass]
	public class ProjectWriterTest
	{
		[TestMethod]
		public void ValidatesFileIsWritable()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				AssemblyAttributes = new AssemblyAttributes(),
				FilePath = new System.IO.FileInfo("test.cs")
			});

			var project = new ProjectReader().Read("TestFiles\\OtherTestProjects\\readonly.testcsproj");

			var messageNum = 0;
			var progress = new Progress<string>(x =>
			{
				if (messageNum++ == 0)
				{
					Assert.AreEqual(
						@"TestFiles\OtherTestProjects\readonly.testcsproj is readonly, please make the file writable first (checkout from source control?).",
						x);
				}
			});
			writer.Write(project, false, progress);
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
			var xmlNode = writer.CreateXml(project);

			var generatedConfigurations = xmlNode.Element("PropertyGroup").Element("Configurations");
			Assert.AreEqual("Debug;Release", generatedConfigurations.Value);
		}

		[TestMethod]
		public void SkipDelaySignNull()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				DelaySign = null,
				FilePath = new System.IO.FileInfo("test.cs")
			});

			var delaySign = xmlNode.Element("PropertyGroup").Element("DelaySign");
			Assert.IsNull(delaySign);
		}

		[TestMethod]
		public void OutputDelaySignTrue()
		{
			var writer = new ProjectWriter();
			var xmlNode = writer.CreateXml(new Project
			{
				DelaySign = true,
				FilePath = new System.IO.FileInfo("test.cs")
			});

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
				DelaySign = false,
				FilePath = new System.IO.FileInfo("test.cs")
			});

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

		[TestMethod]
		public void DeletedFileIsNotCheckedOut()
		{
			var filesToDelete = new FileSystemInfo[]
			{
				new FileInfo(@"TestFiles\Deletions\a.txt"),
				new FileInfo(@"TestFiles\Deletions\AssemblyInfo.txt")
			};

			var assemblyInfoFile = new FileInfo(@"TestFiles\Deletions\AssemblyInfo.txt");

			var actualDeletedFiles = new List<FileSystemInfo>();
			var checkedOutFiles = new List<FileSystemInfo>();

			//Just simulate deletion so we can just check the list
			void Deletion(FileSystemInfo info) => actualDeletedFiles.Add(info);
			void Checkout(FileSystemInfo info) => checkedOutFiles.Add(info);

			var writer = new ProjectWriter(Deletion, Checkout);

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test1.csproj"),
					AssemblyAttributes = new AssemblyAttributes
					{
						File = assemblyInfoFile,
						Company = "A Company"
					},
					Deletions = filesToDelete.ToArray()
				},
				false, new Progress<string>()
			);

			CollectionAssert.AreEqual(filesToDelete, actualDeletedFiles);
			CollectionAssert.DoesNotContain(checkedOutFiles, assemblyInfoFile);
		}

		[TestMethod]
		public void DeletedFileIsProcessed()
		{
			var filesToDelete = new FileSystemInfo[]
			{
				new FileInfo(@"TestFiles\Deletions\a.txt")
			};

			var actualDeletedFiles = new List<FileSystemInfo>();

			//Just simulate deletion so we can just check the list
			void Deletion(FileSystemInfo info) => actualDeletedFiles.Add(info);

			var writer = new ProjectWriter(Deletion, _ => { });

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test1.csproj"),
					Deletions = filesToDelete.ToArray()
				},
				false, new Progress<string>()
			);

			CollectionAssert.AreEqual(filesToDelete, actualDeletedFiles);
		}

		[TestMethod]
		public void DeletedFolderIsProcessed()
		{
			//delete the dummy file we put in to make sure the folder was copied over
			File.Delete(@"TestFiles\Deletions\EmptyFolder\a.txt");

			var filesToDelete = new FileSystemInfo[]
			{
				new DirectoryInfo(@"TestFiles\Deletions\EmptyFolder")
			};

			var actualDeletedFiles = new List<FileSystemInfo>();

			//Just simulate deletion so we can just check the list
			void Deletion(FileSystemInfo info) => actualDeletedFiles.Add(info);

			var writer = new ProjectWriter(Deletion, _ => { });

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test2.csproj"),
					Deletions = filesToDelete.ToArray()
				},
				false, new Progress<string>()
			);

			CollectionAssert.AreEqual(filesToDelete, actualDeletedFiles);
		}

		[TestMethod]
		public void DeletedNonEmptyFolderIsProcessedIfCleared()
		{
			var folder = @"TestFiles\Deletions\NonEmptyFolder";
			var file = @"TestFiles\Deletions\NonEmptyFolder\a.txt";

			var filesToDelete = new FileSystemInfo[]
			{
					new FileInfo(file),
					new DirectoryInfo(folder)
			};

			var actualDeletedFiles = new List<FileSystemInfo>();

			//Just simulate deletion so we can just check the list
			void Deletion(FileSystemInfo info)
			{
				//need to actually delete this one so the folder can be deleted
				info.Delete();
				actualDeletedFiles.Add(info);
			}

			try
			{
				var writer = new ProjectWriter(Deletion, _ => { });

				writer.Write(
					new Project
					{
						FilePath = new FileInfo(@"TestFiles\Deletions\Test3.csproj"),
						Deletions = filesToDelete.ToArray()
					},
					false, new Progress<string>()
				);

				CollectionAssert.AreEqual(filesToDelete, actualDeletedFiles);
			}
			finally
			{
				//Restore the directory and file back to how it was before test
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}

				if (!File.Exists(file))
				{
					File.Create(file);
				}
			}
		}

		[TestMethod]
		public void DeletedNonEmptyFolderIsNotProcessed()
		{
			var filesToDelete = new FileSystemInfo[]
			{
				new DirectoryInfo(@"TestFiles\Deletions\NonEmptyFolder2")
			};

			var actualDeletedFiles = new List<FileSystemInfo>();

			//Just simulate deletion so we can just check the list
			void Deletion(FileSystemInfo info) => actualDeletedFiles.Add(info);

			var writer = new ProjectWriter(Deletion, _ => { });

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test4.csproj"),
					Deletions = filesToDelete.ToArray()
				},
				false, new Progress<string>()
			);

			CollectionAssert.AreEqual(new FileSystemInfo[0], actualDeletedFiles);
		}
	}
}