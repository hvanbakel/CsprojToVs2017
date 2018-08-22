using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017Tests
{
	[TestClass]
	public class TargetFrameworkTransformationTest
	{
		[TestMethod]
		public void HandlesProjectNull()
		{
			Project project = null;
			var targetFrameworks = new List<string> { "netstandard2.0" };

			var transformation = new TargetFrameworkTransformation(targetFrameworks);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.IsNull(project);
		}
		
		[TestMethod]
		public void HandlesProjectTargetFrameworksEmpty()
		{
			var project = new Project();
			var targetFrameworks = new List<string> { "netstandard2.0" };

			var transformation = new TargetFrameworkTransformation(targetFrameworks);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(1, project.TargetFrameworks.Count);
			Assert.AreEqual("netstandard2.0", project.TargetFrameworks[0]);
		}

		[TestMethod]
		public void HandlesOptionTargetFrameworksNull()
		{
			var project = new Project
			{
				TargetFrameworks = { "net46" }
			};
			
			var transformation = new TargetFrameworkTransformation(null);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(1, project.TargetFrameworks.Count);
			Assert.AreEqual("net46", project.TargetFrameworks[0]);
		}

		[TestMethod]
		public void HandlesOptionTargetFrameworksEmpty()
		{
			var project = new Project
			{
				TargetFrameworks = { "net46" }
			};

			var transformation = new TargetFrameworkTransformation(new List<string>());
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(1, project.TargetFrameworks.Count);
			Assert.AreEqual("net46", project.TargetFrameworks[0]);
		}

		[TestMethod]
		public void HandlesOptionTargetFrameworks()
		{
			var project = new Project
			{
				TargetFrameworks = { "net46" }
			};
			var targetFrameworks = new List<string> { "netstandard2.0" };

			var transformation = new TargetFrameworkTransformation(targetFrameworks);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(1, project.TargetFrameworks.Count);
			Assert.AreEqual("netstandard2.0", project.TargetFrameworks[0]);
		}

		[TestMethod]
		public void HandlesOptionTargetFrameworksMulti()
		{
			var project = new Project
			{
				TargetFrameworks = { "net46" }
			};
			var targetFrameworks = new List<string> { "netstandard2.0", "net47" };

			var transformation = new TargetFrameworkTransformation(targetFrameworks);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(2, project.TargetFrameworks.Count);
			Assert.AreEqual("netstandard2.0", project.TargetFrameworks[0]);
			Assert.AreEqual("net47", project.TargetFrameworks[1]);
		}

		[TestMethod]
		public void HandlesOptionAppendTargetFrameworkToOutputPathTrue()
		{
			var project = new Project();

			var transformation = new TargetFrameworkTransformation(null, true);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(true, project.AppendTargetFrameworkToOutputPath);
		}

		[TestMethod]
		public void HandlesOptionAppendTargetFrameworkToOutputPathFalse()
		{
			var project = new Project();

			var transformation = new TargetFrameworkTransformation(null, false);
			transformation.Transform(project, NoopLogger.Instance);

			Assert.AreEqual(false, project.AppendTargetFrameworkToOutputPath);
		}
	}
}