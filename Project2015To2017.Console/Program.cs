using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Logging;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Migrate2017;
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

			logger.LogError("csproj-to-2017 is obsolete and will be removed very soon. Migrate to Project2015To2017.Migrate2017.Tool (dotnet migrate-2017) as soon as possible.");

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
	}
}