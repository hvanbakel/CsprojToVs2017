using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;
using Project2015To2017.Reading;
using Project2015To2017.Writing;

namespace Project2015To2017Tests
{
	[TestClass]
	public class ProjectWriterTest
	{
		[TestMethod]
		public void ValidatesFileIsWritable()
		{
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


			File.SetAttributes("TestFiles\\OtherTestProjects\\readonly.testcsproj", FileAttributes.ReadOnly);

			var writer = new ProjectWriter();

			var project = new ProjectReader().Read("TestFiles\\OtherTestProjects\\readonly.testcsproj");

			writer.Write(project, false, progress);
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

			var writer = new ProjectWriter
			{
				DeleteOperation = Deletion,
				CheckoutOperation = Checkout,
			};

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test1.csproj"),
					AssemblyAttributes = new AssemblyAttributes
					{
						File = assemblyInfoFile,
						Company = "A Company"
					},
					Deletions = filesToDelete.ToList().AsReadOnly()
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

			var writer = new ProjectWriter { DeleteOperation = Deletion };

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test1.csproj"),
					Deletions = filesToDelete.ToList().AsReadOnly()
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

			var writer = new ProjectWriter { DeleteOperation = Deletion };

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test2.csproj"),
					Deletions = filesToDelete.ToList().AsReadOnly()
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
				var writer = new ProjectWriter { DeleteOperation = Deletion };

				writer.Write(
					new Project
					{
						FilePath = new FileInfo(@"TestFiles\Deletions\Test3.csproj"),
						Deletions = filesToDelete.ToList().AsReadOnly()
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

			var writer = new ProjectWriter { DeleteOperation = Deletion };

			writer.Write(
				new Project
				{
					FilePath = new FileInfo(@"TestFiles\Deletions\Test4.csproj"),
					Deletions = filesToDelete.ToList().AsReadOnly()
				},
				false, new Progress<string>()
			);

			CollectionAssert.AreEqual(new FileSystemInfo[0], actualDeletedFiles);
		}
	}
}
