using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;
using Project2015To2017.Writing;

namespace Project2015To2017.Migrate2017.Tool
{
	public class CommandLogic
	{
		private readonly Microsoft.Extensions.Logging.ILogger genericLogger;

		public CommandLogic()
		{
			this.genericLogger = new Serilog.Extensions.Logging.SerilogLoggerProvider().CreateLogger(nameof(Serilog));
		}

		public IReadOnlyCollection<Project> ParseProjects(
			IEnumerable<string> items,
			ITransformationSet transformationSet,
			ConversionOptions conversionOptions)
		{
			var convertedProjects = new List<Project>();

			foreach (var file in items)
			{
				var projects = new ProjectConverter(genericLogger, transformationSet, conversionOptions)
					.Convert(file)
					.Where(x => x != null)
					.ToList();
				convertedProjects.AddRange(projects);
			}

			return convertedProjects;
		}

		public void ExecuteEvaluate(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			var convertedProjects = ParseProjects(items, Vs15TransformationSet.Instance, conversionOptions);

			DoAnalysis(convertedProjects);
		}

		public void ExecuteMigrate(
			IReadOnlyCollection<string> items,
			bool noBackup,
			ConversionOptions conversionOptions)
		{
			var convertedProjects = ParseProjects(items, Vs15TransformationSet.Instance, conversionOptions);

			var doBackup = !noBackup;

			var writer = new ProjectWriter(genericLogger, x => x.Delete(), _ => { });
			foreach (var project in convertedProjects)
			{
				writer.Write(project, doBackup);
			}

			conversionOptions.ProjectCache?.Purge();

			convertedProjects = ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);

			DoAnalysis(convertedProjects);
		}

		public void ExecuteAnalyze(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			var convertedProjects = ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);

			DoAnalysis(convertedProjects);
		}

		private void DoAnalysis(IEnumerable<Project> convertedProjects)
		{
			var analyzer = new Analyzer<LoggerReporter, LoggerReporterOptions>(new LoggerReporter(genericLogger));

			foreach (var project in convertedProjects)
			{
				analyzer.Analyze(project);
			}
		}
	}
}
