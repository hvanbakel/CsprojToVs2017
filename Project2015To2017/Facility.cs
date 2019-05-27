using System;
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

		public readonly ILogger Logger;

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
				self.Logger.LogWarning(
					"Directory {Directory} contains {Count} matching files, specify which project or solution file to use.",
					dir, cwdFiles.Length);
				if (cwdFiles.Length <= 4)
				{
					foreach (var cwdFile in cwdFiles)
					{
						self.Logger.LogInformation(
							"File: {File}",
							cwdFile.FullName);
					}
				}
			}
			return true;
		};

		public Facility(ILogger logger, params PatternProcessor[] additionalProcessors)
		{
			Logger = logger;
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
				Logger.LogTrace("Glob file list reevaluation skipped");
				return;
			}

			Logger.LogTrace("Glob file list reevaluation started");
			Files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
				.Select(x => (path: x, extension: Path.GetExtension(x)?.ToLowerInvariant()))
				.Where(x => !string.IsNullOrEmpty(x.extension))
				.Where(x => Extensions.Contains(x.extension))
				.ToImmutableArray();
			Logger.LogTrace("Glob file list reevaluation finished: {Count} items", Files.Length);
		}

		public void DoAnalysis(IEnumerable<Project> projects, AnalysisOptions options = null)
		{
			Logger.LogTrace("Starting analysis...");
			var analyzer = new Analyzer<LoggerReporter, LoggerReporterOptions>(new LoggerReporter(Logger), options);

			foreach (var project in projects)
			{
				try
				{
					analyzer.Analyze(project);
				}
				catch (Exception e)
				{
					Logger.LogError(e, "Project {Item} analysis has thrown an exception, skipping...",
						project.ProjectName);
				}
			}
		}

		public (IReadOnlyCollection<Project> projects, IReadOnlyCollection<Solution> solutions) ParseProjects(
			IEnumerable<string> items,
			ITransformationSet transformationSet,
			ConversionOptions conversionOptions)
		{
			var converter = new ProjectConverter(Logger, transformationSet, conversionOptions);
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
				Logger.LogTrace("Processing {Item}", file);
				try
				{
					switch (extension)
					{
						case ".sln":
							{
								var solution = SolutionReader.Instance.Read(file, Logger);
								convertedSolutions.Add(solution);
								convertedProjects.AddRange(converter.ProcessSolutionFile(solution).Where(x => x != null));
								break;
							}
						default:
							{
								var converted = converter.ProcessProjectFile(file, null);
								if (converted != null)
								{
									convertedProjects.Add(converted);
								}

								break;
							}
					}
				}
				catch (Exception e)
				{
					Logger.LogError(e, "Project {Item} parsing has thrown an exception, skipping...", file);
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

			var alreadyConverted = projects.Where(x => x.IsModernProject).ToImmutableArray();

			DoAnalysis(projects, new AnalysisOptions(Vs15DiagnosticSet.All));

			Logger.LogInformation("List of modern CPS projects:");
			foreach (var projectPath in alreadyConverted.Select(x => x.FilePath))
			{
				Logger.LogInformation("Project {ProjectPath} is already CPS-based", projectPath);
			}

			var projectPaths = solutions.SelectMany(x => x.UnsupportedProjectPaths).ToImmutableArray();
			if (projectPaths.Length <= 0) return;

			Logger.LogInformation("List of unsupported solution projects:");
			foreach (var projectPath in projectPaths)
			{
				Logger.LogWarning("Project {ProjectPath} not supported", projectPath);
			}
		}

		public void ExecuteMigrate(
			IReadOnlyCollection<string> items,
			ITransformationSet transformations
		)
		{
			ExecuteMigrate(items, transformations, new ConversionOptions(), new ProjectWriteOptions());
		}

		public void ExecuteMigrate(
				IReadOnlyCollection<string> items,
				ConversionOptions conversionOptions,
				ProjectWriteOptions writeOptions
			)
		{
			ExecuteMigrate(items, Vs15TransformationSet.Instance, conversionOptions, writeOptions);
		}

		public void ExecuteMigrate(
			IReadOnlyCollection<string> items,
			ITransformationSet transformations,
			ConversionOptions conversionOptions,
			ProjectWriteOptions writeOptions
			)
		{
			var (projects, _) = ParseProjects(items, transformations, conversionOptions);

			if (projects.Count == 0)
			{
				return;
			}

			var writer = new ProjectWriter(Logger, writeOptions);
			foreach (var project in projects)
			{
				writer.TryWrite(project);
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