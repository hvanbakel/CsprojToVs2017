using System;
using DotNet.Globbing;
using Serilog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;
using Project2015To2017.Writing;
using Project2015To2017.Migrate2017;

namespace Project2015To2017.Migrate2017.Tool
{
	public class CommandLogic
	{
		private readonly PatternProcessor globProcessor = (converter, pattern, callback, self) =>
		{
			Log.Verbose("Falling back to globbing");
			self.DoProcessableFileSearch();
			var glob = Glob.Parse(pattern);
			Log.Verbose("Parsed glob {Glob}", glob);
			foreach (var (path, extension) in self.Files)
			{
				if (!glob.IsMatch(path)) continue;
				var file = new FileInfo(path);
				callback(file, extension);
			}

			return true;
		};

		private readonly MigrationFacility facility;

		public CommandLogic()
		{
			var genericLogger = new Serilog.Extensions.Logging.SerilogLoggerProvider().CreateLogger(nameof(Serilog));
			facility = new MigrationFacility(genericLogger, globProcessor);
		}

		public void ExecuteEvaluate(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			facility.ExecuteEvaluate(items, conversionOptions);
		}

		public void ExecuteMigrate(
			IReadOnlyCollection<string> items,
			bool noBackup,
			ConversionOptions conversionOptions)
		{
			var writeOptions = new ProjectWriteOptions { MakeBackups = !noBackup };
			facility.ExecuteMigrate(items, conversionOptions, writeOptions);
		}

		public void ExecuteAnalyze(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			facility.ExecuteAnalyze(items, conversionOptions);
		}

		public void ExecuteWizard(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			conversionOptions.UnknownTargetFrameworkCallback = WizardUnknownTargetFrameworkCallback;

			var (projects, solutions) =
				facility.ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);

			if (projects.Count == 0)
			{
				Log.Information("No projects have been found to match your criteria.");
				return;
			}

			var (modern, legacy) = projects.Split(x => x.IsModernProject);
			foreach (var projectPath in modern.Select(x => x.FilePath))
			{
				Log.Information("Project {ProjectPath} is already CPS-based", projectPath);
			}

			foreach (var projectPath in solutions.SelectMany(x => x.UnsupportedProjectPaths))
			{
				Log.Warning("Project {ProjectPath} migration is not supported at the moment",
					projectPath);
			}

			facility.DoAnalysis(projects, new AnalysisOptions(DiagnosticSet.All));

			if (legacy.Count > 0)
			{
				var doBackups = AskBinaryChoice("Would you like to create backups?");

				var writer = new ProjectWriter(facility.Logger, new ProjectWriteOptions { MakeBackups = doBackups });

				foreach (var project in legacy)
				{
					using (facility.Logger.BeginScope(project.FilePath))
					{
						var projectName = Path.GetFileNameWithoutExtension(project.FilePath.Name);
						Log.Information("Converting {ProjectName}...", projectName);

						if (!project.Valid)
						{
							Log.Error("Project {ProjectName} is marked as invalid, skipping...", projectName);
							continue;
						}

						foreach (var transformation in Vs15TransformationSet.TrueInstance.IterateTransformations(
							facility.Logger,
							conversionOptions))
						{
							try
							{
								transformation.Transform(project);
							}
							catch (Exception e)
							{
								Log.Error(e, "Transformation {Item} has thrown an exception, skipping...",
									transformation.GetType().Name);
							}
						}

						if (!writer.TryWrite(project))
							continue;
						Log.Information("Project {ProjectName} has been converted", projectName);
					}
				}
			}
			else
			{
				var writer = new ProjectWriter(facility.Logger, new ProjectWriteOptions());

				Log.Information("It appears you already have everything converted to CPS.");
				if (AskBinaryChoice("Would you like to process CPS projects to clean up and reformat them?"))
				{
					foreach (var project in modern)
					{
						using (facility.Logger.BeginScope(project.FilePath))
						{
							var projectName = Path.GetFileNameWithoutExtension(project.FilePath.Name);
							Log.Information("Processing {ProjectName}...", projectName);

							if (!project.Valid)
							{
								Log.Error("Project {ProjectName} is marked as invalid, skipping...", projectName);
								continue;
							}

							foreach (var transformation in Vs15TransformationSet.TrueInstance.IterateTransformations(
								facility.Logger,
								conversionOptions))
							{
								try
								{
									transformation.Transform(project);
								}
								catch (Exception e)
								{
									Log.Error(e, "Transformation {Item} has thrown an exception, skipping...",
										transformation.GetType().Name);
								}
							}

							if (!writer.TryWrite(project))
								continue;
							Log.Information("Project {ProjectName} has been processed", projectName);
						}
					}
				}
			}

			conversionOptions.ProjectCache?.Purge();

			(projects, _) = facility.ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);

			Log.Information("Modernization can be progressed a little further, but it might lead to unexpected behavioral changes.");
			if (AskBinaryChoice("Would you like to modernize projects?"))
			{
				var doBackups = AskBinaryChoice("Would you like to create backups?");

				var writer = new ProjectWriter(facility.Logger, new ProjectWriteOptions { MakeBackups = doBackups });

				foreach (var project in projects)
				{
					using (facility.Logger.BeginScope(project.FilePath))
					{
						var projectName = Path.GetFileNameWithoutExtension(project.FilePath.Name);
						Log.Information("Modernizing {ProjectName}...", projectName);

						if (!project.Valid)
						{
							Log.Error("Project {ProjectName} is marked as invalid, skipping...", projectName);
							continue;
						}

						foreach (var transformation in Vs15ModernizationTransformationSet.TrueInstance
							.IterateTransformations(
								facility.Logger,
								conversionOptions))
						{
							try
							{
								transformation.Transform(project);
							}
							catch (Exception e)
							{
								Log.Error(e, "Transformation {Item} has thrown an exception, skipping...",
									transformation.GetType().Name);
							}
						}

						if (!writer.TryWrite(project))
							continue;
						Log.Information("Project {ProjectName} has been modernized", projectName);
					}
				}

				conversionOptions.ProjectCache?.Purge();

				(projects, _) = facility.ParseProjects(items, BasicReadTransformationSet.Instance, conversionOptions);
			}

			var diagnostics = new DiagnosticSet(Vs15DiagnosticSet.All);
			diagnostics.ExceptWith(DiagnosticSet.All);
			facility.DoAnalysis(projects, new AnalysisOptions(diagnostics));
		}

		private readonly List<(ImmutableHashSet<(string when, string tfms)> variant, ImmutableArray<string> answer)>
			previousFrameworkResolutionAnswers =
				new List<(ImmutableHashSet<(string, string)>, ImmutableArray<string>)>();

		private bool WizardUnknownTargetFrameworkCallback(Project project,
			IReadOnlyList<(IReadOnlyList<string> frameworks, XElement source, string condition)> foundTargetFrameworks)
		{
			Log.Warning("Cannot unambiguously determine target frameworks for {ProjectName}.", project.FilePath);

			var currentVariant = new HashSet<(string when, string tfms)>();

			if (foundTargetFrameworks.Count > 0)
			{
				Log.Information("Found {Count} possible variants:", foundTargetFrameworks.Count);
				foreach (var (frameworks, source, condition) in foundTargetFrameworks)
				{
					if (source is IXmlLineInfo elementOnLine && elementOnLine.HasLineInfo())
					{
						Log.Information("{Line}: {TFMs} ({When})", elementOnLine.LineNumber, frameworks, condition);
					}
					else
					{
						Log.Information("{TFMs} ({When})", frameworks, condition);
					}

					currentVariant.Add((condition,
						string.Join(";", frameworks.ToImmutableSortedSet()).ToLowerInvariant()));
				}

				foreach (var (variant, answer) in previousFrameworkResolutionAnswers)
				{
					if (!currentVariant.SetEquals(variant))
						continue;

					Log.Information("You have previously selected {FormerChoice} for that combination.", answer);
					if (AskBinaryChoice("Would you like to use this framework set?"))
					{
						foreach (var tfm in answer)
						{
							project.TargetFrameworks.Add(tfm);
						}

						return true;
					}

					break;
				}
			}

			Log.Information("Please, enter target frameworks to use (comma or space separated):");
			Console.Out.Flush();
			var tfms = Console.ReadLine()
						   ?.Trim()
						   .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
						   .Where(s => !string.IsNullOrWhiteSpace(s))
						   .ToImmutableArray() ?? ImmutableArray<string>.Empty;

			if (tfms.IsDefaultOrEmpty)
			{
				Log.Warning("You didn't specify any TFMs to use.");
				return AskBinaryChoice("Attempt to continue without specifying them?", defaultChoiceIsYes: false);
			}

			if (foundTargetFrameworks.Count > 0)
			{
				previousFrameworkResolutionAnswers.Add((currentVariant.ToImmutableHashSet(), tfms));
			}

			foreach (var tfm in tfms)
			{
				project.TargetFrameworks.Add(tfm);
			}

			return true;
		}

		private static bool AskBinaryChoice(string question, string yes = "Yes", string no = "No",
			bool defaultChoiceIsYes = true)
		{
			Console.Out.Flush();
			var yesCharLower = char.ToLowerInvariant(yes[0]);
			var noCharLower = char.ToLowerInvariant(no[0]);
			var yesChar = defaultChoiceIsYes ? char.ToUpperInvariant(yes[0]) : yesCharLower;
			var noChar = defaultChoiceIsYes ? noCharLower : char.ToUpperInvariant(no[0]);
			Console.Write($"{question} ({yesChar}/{noChar}) ");
			Console.Out.Flush();
			bool? result = null;
			while (!result.HasValue)
			{
				result = DetermineKeyChoice(Console.ReadKey(true), yesCharLower, noCharLower, defaultChoiceIsYes);
			}

			var realResult = result.Value;
			Console.WriteLine(realResult ? yes : no);
			Console.Out.Flush();
			return realResult;
		}

		private static bool? DetermineKeyChoice(ConsoleKeyInfo info, char yesChar, char noChar, bool defaultChoice)
		{
			switch (char.ToLowerInvariant(info.KeyChar))
			{
				case 'y':
				case 't':
				case '1':
				case char c when c == yesChar:
					return true;
				case 'n':
				case 'f':
				case '0':
				case char c when c == noChar:
					return false;
			}

			switch (info.Key)
			{
				case ConsoleKey.LeftArrow:
					return true;
				case ConsoleKey.RightArrow:
					return false;
				case ConsoleKey.Enter:
					return defaultChoice;
			}

			return null;
		}
	}
}