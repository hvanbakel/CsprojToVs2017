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
		private class SyncProgress : IProgress<string>
		{
			private Action<string> Action { get; }

			public SyncProgress(Action<string> action)
			{
				Action = action;
			}

			public void Report(string value)
			{
				Action(value);
			}
		}

		[TestMethod]
		public void ValidatesFileIsWritable()
		{
			var logs = new List<string>();

			var progress = new SyncProgress(logs.Add);

			var projectFile = Path.Combine("TestFiles", "OtherTestProjects", "readonly.testcsproj");
			var copiedProjectFile = Path.Combine("TestFiles", "OtherTestProjects", $"{nameof(ValidatesFileIsWritable)}.readonly");

			if (File.Exists(copiedProjectFile))
			{
				File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
				File.Delete(copiedProjectFile);
			}

			try
			{
				File.Copy(projectFile, copiedProjectFile);

				File.SetAttributes(copiedProjectFile, FileAttributes.ReadOnly);

				var project = new ProjectReader(copiedProjectFile, progress).Read();

				Assert.IsFalse(logs.Any(x => x.Contains("Aborting as could not write to project file")));

				var writer = new ProjectWriter();

				writer.Write(project, makeBackups: false, progress);

				Assert.IsTrue(logs.Any(x => x.Contains("Aborting as could not write to project file")));
			}
			finally
			{
				if (File.Exists(copiedProjectFile))
				{
					File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
					File.Delete(copiedProjectFile);
				}
			}
		}

		[TestMethod]
		public void ValidatesFileIsWritableAfterCheckout()
		{
			var logs = new List<string>();

			var progress = new SyncProgress(logs.Add);

			var projectFile = Path.Combine("TestFiles", "OtherTestProjects", "readonly.testcsproj");
			var copiedProjectFile = Path.Combine("TestFiles", "OtherTestProjects", $"{nameof(ValidatesFileIsWritableAfterCheckout)}.readonly");

			if (File.Exists(copiedProjectFile))
			{
				File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
				File.Delete(copiedProjectFile);
			}

			try
			{
				File.Copy(projectFile, copiedProjectFile);

				File.SetAttributes(copiedProjectFile, FileAttributes.ReadOnly);

				var project = new ProjectReader(copiedProjectFile, progress).Read();

				var projectWriter = new ProjectWriter(_ => { }, file => File.SetAttributes(file.FullName, FileAttributes.Normal));

				projectWriter.Write(project, makeBackups: false, progress);

				Assert.IsFalse(logs.Any(x => x.Contains("Aborting as could not write to project file")));
			}
			finally
			{
				if (File.Exists(copiedProjectFile))
				{
					File.SetAttributes(copiedProjectFile, FileAttributes.Normal);
					File.Delete(copiedProjectFile);
				}
			}
		}

		[TestMethod]
		public void ValidatesFileExists()
		{
			var progress = new Progress<string>(x => { });

			Assert.IsFalse(ProjectConverter.Validate(new FileInfo(Path.Combine("TestFiles", "OtherTestProjects", "nonexistent.testcsproj")), progress));
		}
	}
}
