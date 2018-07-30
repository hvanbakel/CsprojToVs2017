using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public class SolutionReader
	{
		public static readonly SolutionReader Instance = new SolutionReader();

		public Solution Read(string filePath)
		{
			return Read(filePath, new Progress<string>(_ => { }));
		}

		public Solution Read(string filePath, IProgress<string> progress)
		{
			var fileInfo = new FileInfo(filePath);
			var projectPaths = new List<ProjectReference>();
			using (var reader = new StreamReader(fileInfo.OpenRead()))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (!line.Trim().StartsWith("Project(", StringComparison.Ordinal))
					{
						continue;
					}

					var projectPath = line.Split('"').FirstOrDefault(x => x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
					if (projectPath != null)
					{
						var reference = new ProjectReference
						{
							Include = projectPath,
							ProjectFile = new FileInfo(Path.Combine(fileInfo.Directory.FullName, projectPath)),
						};
						projectPaths.Add(reference);
					}
				}
			}

			var solutionDefinition = new Solution
			{
				FilePath = fileInfo,
				ProjectPaths = projectPaths
			};

			return solutionDefinition;
		}
	}
}
