using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Project2015To2017
{
	public static class ProjectConverterExtensions
	{
		public static IEnumerable<Project> Convert(this ProjectConverter self, string target, ILogger logger = default)
		{
			if (logger == null) logger = NoopLogger.Instance;

			var extension = Path.GetExtension(target) ?? throw new ArgumentNullException(nameof(target));
			if (extension.Length > 0)
			{
				var file = new FileInfo(target);

				switch (extension)
				{
					case ".sln":
						{
							var solution = SolutionReader.Instance.Read(file, logger);
							foreach (var project in self.ProcessSolutionFile(solution))
							{
								yield return project;
							}
							break;
						}
					case string s when ProjectConverter.ProjectFileMappings.ContainsKey(extension):
						{
							yield return self.ProcessProjectFile(file, null);
							break;
						}
					default:
						logger.LogCritical("Please specify a project or solution file.");
						break;
				}

				yield break;
			}

			// Process the only solution in given directory
			var solutionFiles = Directory.EnumerateFiles(target, "*.sln", SearchOption.TopDirectoryOnly).ToArray();
			if (solutionFiles.Length == 1)
			{
				var solution = SolutionReader.Instance.Read(solutionFiles[0], logger);
				foreach (var project in self.ProcessSolutionFile(solution))
				{
					yield return project;
				}

				yield break;
			}

			var projectsProcessed = 0;
			// Process all csprojs found in given directory
			foreach (var fileExtension in ProjectConverter.ProjectFileMappings.Keys)
			{
				var projectFiles = Directory.EnumerateFiles(target, "*" + fileExtension, SearchOption.AllDirectories).ToArray();
				if (projectFiles.Length == 0)
				{
					continue;
				}

				if (projectFiles.Length > 1)
				{
					logger.LogInformation($"Multiple project files found under directory {target}:");
				}

				logger.LogInformation(string.Join(Environment.NewLine, projectFiles));

				foreach (var projectFile in projectFiles)
				{
					// todo: rewrite both directory enumerations to use FileInfo instead of raw strings
					yield return self.ProcessProjectFile(new FileInfo(projectFile), null);
					projectsProcessed++;
				}
			}

			if (projectsProcessed == 0)
			{
				logger.LogCritical("Please specify a project file.");
			}
		}
	}
}
