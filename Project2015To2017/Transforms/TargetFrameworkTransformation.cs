using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
    public sealed class TargetFrameworkTransformation : ITransformation
	{
		public TargetFrameworkTransformation(IReadOnlyList<string> targetFrameworks)
		{
			TargetFrameworks = targetFrameworks;
		}

		public void Transform(Project definition, IProgress<string> progress)
		{
			if (null != TargetFrameworks && TargetFrameworks.Any())
			{
				definition.TargetFrameworks = TargetFrameworks;
			}
		}

		public IReadOnlyList<string> TargetFrameworks { get; }
	}
}
