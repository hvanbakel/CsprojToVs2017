using Microsoft.Extensions.Logging;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2017;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using Project2015To2017.Writing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Project2015To2017
{
	public class Facility
	{
		// ReSharper disable MemberCanBePrivate.Global
		/// <summary>
		/// Supported files to be considered for migration/analysis
		/// </summary>
		public ImmutableArray<(string path, string extension)> Files;
		/// <summary>
		/// Supported project/solution file extensions
		/// </summary>
		public ImmutableArray<string> Extensions;
		/// <summary>
		/// The ordered list of processors applied to each supported file until one returns true
		/// </summary>
		public readonly List<PatternProcessor> Processors;
		// ReSharper restore MemberCanBePrivate.Global

		private readonly ILogger logger;

		private readonly PatternProcessor fileProcessor = (converter, pattern, callback, _) =>
		{
			if (!File.Exists(pattern)) return false;

			var file = new FileInfo(pattern);
			var extension = file.Extension.ToLowerInvariant();
			callback(file, extension);
			return true;
		};

		private readonly PatternProcessor directoryProcessor = (converter, pattern, callback, self) =>
		{
			if (!Directory.Exists(pattern)) return false;

			var dir = new DirectoryInfo(pattern);
			var cwdFiles = dir.GetFiles()
				.Where(x => self.Extensions.Contains(x.Extension.ToLowerInvariant()))
				.ToImmutableArray();
			if (cwdFiles.Length == 1)
			{
				var file = cwdFiles[0];
				var extension = file.Extension.ToLowerInvariant();
				callback(file, extension);
			}
			else
			{
				self.logger.LogWarning(
					"Directory {Directory} contains {Count} matching files, specify which project or solution file to use.",
					dir, cwdFiles.Length);
			}
			return true;
		};

		public Facility(ILogger logger, params PatternProcessor[] additionalProcessors)
		{
			this.logger = logger;
			Extensions = ProjectConverter.ProjectFileMappings.Keys.Concat(new[] { ".sln" }).ToImmutableArray();
			Processors = new List<PatternProcessor>(2 + additionalProcessors.Length)
			{
				fileProcessor, directoryProcessor
			};
			Processors.AddRange(additionalProcessors);
		}

		public void DoProcessableFileSearch(bool force = false)
		{
			if (Files != null && Files.Length > 0 && !force)
			{
				logger.LogTrace("Glob file list reevaluation skipped");
				return;
			}

			logger.LogTrace("Glob file list reevaluation started");
			Files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
				.Select(x => (path: x, extension: Path.GetExtension(x)?.ToLowerInvariant()))
				.Where(x => !string.IsNullOrEmpty(x.extension))
				.Where(x => Extensions.Contains(x.extension))
				.ToImmutableArray();
			logger.LogTrace("Glob file list reevaluation finished: {Count} items", Files.Length);
		}

		private void DoAnalysis(IEnumerable<Project> convertedProjects, AnalysisOptions options = null)
		{
			logger.LogTrace("Starting analysis...");
			var analyzer = new Analyzer<LoggerReporter, LoggerReporterOptions>(new LoggerReporter(logger), options);

			foreach (var project in convertedProjects)
			{
				analyzer.Analyze(project);
			}
		}

		public (IReadOnlyCollection<Project> projects, IReadOnlyCollection<Solution> solutions) ParseProjects(
			IEnumerable<string> items,
			ITransformationSet transformationSet,
			ConversionOptions conversionOptions)
		{
			var converter = new ProjectConverter(logger, transformationSet, conversionOptions);
			var convertedProjects = new List<Project>();
			var convertedSolutions = new List<Solution>();

			foreach (var pattern in items)
			{
				foreach (var patternProcessor in Processors)
				{
					if (patternProcessor?.Invoke(converter, pattern, ProcessSingleItem, this) ?? false)
						break;
				}
			}

			return (convertedProjects, convertedSolutions);

			void ProcessSingleItem(FileInfo file, string extension)
			{
				logger.LogTrace("Processing {Item}", file);
				switch (extension)
				{
					case ".sln":
						{
							var solution = SolutionReader.Instance.Read(file, logger);
							convertedSolutions.Add(solution);
							convertedProjects.AddRange(converter.ProcessSolutionFile(solution));
							break;
						}
					default:
						{
							convertedProjects.Add(converter.ProcessProjectFile(file, null));
							break;
						}
				}
			}
		}

		public void ExecuteEvaluate(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			var (projects, solutions) = ParseProjects(items, Vs15TransformationSet.Instance, conversionOptions);

			if (projects.Count == 0)
			{
				return;
			}

			DoAnalysis(projects, new AnalysisOptions(DiagnosticSet.All));

			var projectPaths = solutions.SelectMany(x => x.UnsupportedProjectPaths).ToImmutableArray();
			if (projectPaths.Length <= 0) return;

			logger.LogInformation("List of unsupported solution projects:");
			foreach (var projectPath in projectPaths)
			{
				logger.LogWarning("Project {ProjectPath} not supported", projectPath);
			}
		}

		public void ExecuteMigrate(
			IReadOnlyCollection<string> items,
			bool noBackup,
			ConversionOptions conversionOptions)
		{
			var (projects, _) = ParseProjects(items, Vs15TransformationSet.Instance, conversionOptions);

			if (projects.Count == 0)
			{
				return;
			}

			var doBackup = !noBackup;

			var writer = new ProjectWriter(logger, x => x.Delete(), _ => { });
			foreach (var project in projects)
			{
				writer.Write(project, doBackup);
			}

			conversionOptions.ProjectCache?.Purge();

			(projects, _) = ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);

			DoAnalysis(projects);
		}

		public void ExecuteAnalyze(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			var (projects, _) = ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);

			if (projects.Count == 0)
			{
				return;
			}

			DoAnalysis(projects);
		}
	}
}
