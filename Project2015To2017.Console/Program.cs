using CommandLine;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Project2015To2017.Console
{
	internal static class Program
	{
		static void Main(string[] args)
		{
			ProjectReader.EnableCaching = true;
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(ConvertProject);
		}

		private static void ConvertProject(Options options)
		{
#if DEBUG
			var progress = new Progress<string>(x => Debug.WriteLine(x));
#else
			var progress = new Progress<string>(System.Console.WriteLine);
#endif

			var conversionOptions = options.ConversionOptions;

			var convertedProjects = new List<Project>();

			foreach (var file in options.Files)
			{
				var projects = ProjectConverter
					.Convert(file, conversionOptions, progress)
					.Where(x => x != null)
					.ToList();
				convertedProjects.AddRange(projects);
			}

			System.Console.Out.Flush();

			var analyzer = new Analyzer();
			foreach (var project in convertedProjects)
			{
				analyzer.Analyze(project);
			}

			if (options.DryRun)
			{
				return;
			}

			var doBackup = !options.NoBackup;

			var writer = new Writing.ProjectWriter(x => x.Delete(), _ => { });
			foreach (var project in convertedProjects)
			{
				if (project.IsModernProject)
				{
					if (progress is IProgress<string> progressImpl)
					{
						progressImpl.Report($"Skipping CPS project '{project.FilePath.Name}'...");
					}

					continue;
				}

				writer.Write(project, doBackup, progress);
			}

			System.Console.Out.Flush();

			if (progress is IProgress<string> progressInterface)
			{
				progressInterface.Report("### Performing 2nd pass to analyze converted projects...");
			}

			ProjectReader.PurgeCache();
			convertedProjects.Clear();

			foreach (var file in options.Files)
			{
				var projects = ProjectConverter
					.Convert(file, conversionOptions, progress)
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