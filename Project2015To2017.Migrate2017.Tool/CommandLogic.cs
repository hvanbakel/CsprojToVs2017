using DotNet.Globbing;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using Project2015To2017.Writing;
using Serilog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Project2015To2017.Migrate2017.Tool
{
	public class CommandLogic
	{
		private readonly Microsoft.Extensions.Logging.ILogger genericLogger;
		private ImmutableArray<(string path, string extension)> files;
		private readonly ImmutableArray<string> extensions;

		public CommandLogic()
		{
			this.genericLogger = new Serilog.Extensions.Logging.SerilogLoggerProvider().CreateLogger(nameof(Serilog));
			extensions = ProjectConverter.ProjectFileMappings.Keys.Concat(new[] {".sln"}).ToImmutableArray();
		}

		public void DoProcessableFileSearch(bool force = false)
		{
			if (files != null && files.Length > 0 && !force)
			{
				Log.Verbose("Glob file list reevaluation skipped");
				return;
			}

			Log.Verbose("Glob file list reevaluation started");
			files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
				.Select(x => (path: x, extension: Path.GetExtension(x)?.ToLowerInvariant()))
				.Where(x => !string.IsNullOrEmpty(x.extension))
				.Where(x => extensions.Contains(x.extension))
				.ToImmutableArray();
			Log.Verbose("Glob file list reevaluation finished: {Count} items", files.Length);
		}

		public (IReadOnlyCollection<Project> projects, IReadOnlyCollection<Solution> solutions) ParseProjects(
			IEnumerable<string> items,
			ITransformationSet transformationSet,
			ConversionOptions conversionOptions)
		{
			var converter = new ProjectConverter(genericLogger, transformationSet, conversionOptions);
			var convertedProjects = new List<Project>();
			var convertedSolutions = new List<Solution>();

			foreach (var pattern in items)
			{
				if (File.Exists(pattern))
				{
					var file = new FileInfo(pattern);
					var extension = file.Extension.ToLowerInvariant();
					ProcessSingleItem(file, extension);
					continue;
				}

				if (Directory.Exists(pattern))
				{
					var dir = new DirectoryInfo(pattern);
					var cwdFiles = dir.GetFiles()
						.Where(x => extensions.Contains(x.Extension.ToLowerInvariant()))
						.ToImmutableArray();
					if (cwdFiles.Length == 1)
					{
						var file = cwdFiles[0];
						var extension = file.Extension.ToLowerInvariant();
						ProcessSingleItem(file, extension);
					}
					else
					{
						Log.Warning(
							"Directory {Directory} contains {Count} matching files, specify which project or solution file to use.",
							dir, cwdFiles.Length);
					}

					continue;
				}

				Log.Verbose("Falling back to globbing");
				DoProcessableFileSearch();
				var glob = Glob.Parse(pattern);
				Log.Verbose("Parsed glob {Glob}", glob);
				foreach (var (path, extension) in files)
				{
					if (!glob.IsMatch(path)) continue;
					var file = new FileInfo(path);
					ProcessSingleItem(file, extension);
				}
			}

			return (convertedProjects, convertedSolutions);

			void ProcessSingleItem(FileInfo file, string extension)
			{
				Log.Verbose("Processing {Item}", file);
				switch (extension)
				{
					case ".sln":
					{
						var solution = SolutionReader.Instance.Read(file, genericLogger);
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

			var diagnosticSet = DiagnosticSet.NoneDefault;
			diagnosticSet.Add(DiagnosticSet.W001);
			diagnosticSet.Add(DiagnosticSet.W010);
			diagnosticSet.Add(DiagnosticSet.W011);
			DoAnalysis(projects, new AnalysisOptions(diagnosticSet));

			var projectPaths = solutions.SelectMany(x => x.UnsupportedProjectPaths).ToImmutableArray();
			if (projectPaths.Length <= 0) return;
			Log.Information("List of unsupported solution projects:");
			foreach (var projectPath in projectPaths)
			{
				Log.Warning("Project {ProjectPath} not supported", projectPath);
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

			var writer = new ProjectWriter(genericLogger, x => x.Delete(), _ => { });
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

		private void DoAnalysis(IEnumerable<Project> convertedProjects, AnalysisOptions options = null)
		{
			Log.Verbose("Starting analysis...");
			var analyzer = new Analyzer<LoggerReporter, LoggerReporterOptions>(new LoggerReporter(genericLogger), options);

			foreach (var project in convertedProjects)
			{
				analyzer.Analyze(project);
			}
		}
	}
}