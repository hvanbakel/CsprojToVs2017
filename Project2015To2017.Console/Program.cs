using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Logging;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

namespace Project2015To2017.Console
{
	internal static class Program
	{
		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(ConvertProject);
		}

		private static void ConvertProject(Options options)
		{
			var conversionOptions = options.ConversionOptions;

			var convertedProjects = new List<Project>();

			ILogger logger = new ConsoleLogger("console", (s, l) => l >= LogLevel.Information, true);

			logger.LogWarning("csproj-to-2017 is deprecated and will be removed soon");
			logger.LogInformation("Consider migrating to Project2015To2017.Migrate2017.Tool (dotnet migrate-2017)");

			foreach (var file in options.Files)
			{
				var projects = new ProjectConverter(logger, Vs15TransformationSet.Instance, conversionOptions)
					.Convert(file, logger)
					.Where(x => x != null)
					.ToList();
				convertedProjects.AddRange(projects);
			}

			System.Console.Out.Flush();

			var analyzer = new Analyzer<LoggerReporter, LoggerReporterOptions>(new LoggerReporter(logger));
			foreach (var project in convertedProjects)
			{
				analyzer.Analyze(project);
			}

			if (options.DryRun)
			{
				return;
			}

			var doBackup = !options.NoBackup;

			var writer = new Writing.ProjectWriter(logger, x => x.Delete(), _ => { });
			foreach (var project in convertedProjects)
			{
				writer.Write(project, doBackup);
			}

			System.Console.Out.Flush();

			logger.LogInformation("### Performing 2nd pass to analyze converted projects...");

			conversionOptions.ProjectCache?.Purge();

			convertedProjects.Clear();

			foreach (var file in options.Files)
			{
				var projects = new ProjectConverter(logger, BasicReadTransformationSet.Instance, conversionOptions)
					.Convert(file, logger)
					.Where(x => x != null)
					.ToList();
				convertedProjects.AddRange(projects);
			}

			System.Console.Out.Flush();

			foreach (var project in convertedProjects)
			{
				analyzer.Analyze(project);
			}
		}

		private static IEnumerable<Project> Convert(this ProjectConverter self, string target, ILogger logger)
		{
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