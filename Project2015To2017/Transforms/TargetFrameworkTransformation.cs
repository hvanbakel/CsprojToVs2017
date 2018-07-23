using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class TargetFrameworkTransformation : ITransformation
	{
		public TargetFrameworkTransformation(IReadOnlyList<string> targetFrameworks)
			: this(targetFrameworks, true)
		{
		}
		public TargetFrameworkTransformation(IReadOnlyList<string> targetFrameworks, bool appendTargetFrameworkToOutputPath)
		{
			TargetFrameworks = targetFrameworks;
			AppendTargetFrameworkToOutputPath = appendTargetFrameworkToOutputPath;
		}

		public void Transform(Project definition, IProgress<string> progress)
		{
			if (null == definition)
				return;
			if (null != TargetFrameworks && TargetFrameworks.Any())
			{
				definition.TargetFrameworks.Clear();
				foreach (var targetFramework in TargetFrameworks)
				{
					definition.TargetFrameworks.Add(targetFramework);
				}
			}
			definition.AppendTargetFrameworkToOutputPath = AppendTargetFrameworkToOutputPath;
		}

		public IReadOnlyList<string> TargetFrameworks { get; }
		public bool AppendTargetFrameworkToOutputPath { get; }
	}
}
