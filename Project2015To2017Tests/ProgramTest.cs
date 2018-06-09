using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Reading;
using Project2015To2017.Writing;

namespace Project2015To2017Tests
{
	[TestClass]
	public class ProgramTest
	{
		[TestMethod]
		public void ValidatesFileIsWritable()
		{
			var logs = new List<string>();

			var progress = new Progress<string>(logs.Add);

			var projectFile = "TestFiles\\OtherTestProjects\\readonly.testcsproj";
			File.SetAttributes(projectFile, FileAttributes.ReadOnly);

			var project = new ProjectReader().Read(projectFile);

			new ProjectWriter().Write(project, makeBackups: false, progress);

			Assert.IsTrue(logs.Any(x => x.Contains("Aborting as could not write to project file")));
		}

		[TestMethod]
		public void ValidatesFileIsWritableAfterCheckout()
		{
			var logs = new List<string>();

			var progress = new Progress<string>(logs.Add);

			var projectFile = "TestFiles\\OtherTestProjects\\readonly.testcsproj";

			File.SetAttributes(projectFile, FileAttributes.ReadOnly);

			var project = new ProjectReader().Read(projectFile);

			var projectWriter = new ProjectWriter
			{
				CheckoutOperation = file => File.SetAttributes(projectFile, FileAttributes.Normal)
			};

			projectWriter.Write(project, makeBackups: false, progress);

			Assert.IsFalse(logs.Any(x => x.Contains("Aborting as could not write to project file")));
		}

		[TestMethod]
		public void ValidatesFileExists()
		{

			var progress = new Progress<string>(x => { });

			Assert.IsFalse(ProjectConverter.Validate(new FileInfo("TestFiles\\OtherTestProjects\\nonexistent.testcsproj"), progress));
		}
	}
}
