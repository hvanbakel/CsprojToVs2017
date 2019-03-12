using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Project2015To2017.Caching;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using static Microsoft.DotNet.Cli.CommandLine.Accept;
using static Microsoft.DotNet.Cli.CommandLine.Create;

namespace Project2015To2017.Migrate2017.Tool
{
	internal static class Program
	{
		static int Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithDemystifiedStackTraces()
				.MinimumLevel.ControlledBy(verbosity)
				.WriteTo.Console()
				.CreateLogger();

			try
			{
				var result = Instance.Parse(args);
				return ProcessArgs(result);
			}
			catch (HelpException e)
			{
				Log.Information(e.Message);
				return 0;
			}
			catch (Exception e)
			{
				if (Log.IsEnabled(LogEventLevel.Debug))
					Log.Fatal(e, "Fatal exception occurred");
				else
					Log.Fatal(e.Message);
				if (e is CommandParsingException commandParsingException)
				{
					Log.Information(commandParsingException.HelpText);
				}

				return 1;
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static readonly LoggingLevelSwitch verbosity = new LoggingLevelSwitch();

		private static int ProcessArgs(ParseResult result)
		{
			result.ShowHelpOrErrorIfAppropriate();

			var command = result.AppliedCommand();
			var verbosityValue = result["dotnet-migrate-2017"].ValueOrDefault<string>("verbosity")?.Trim().ToLowerInvariant() ?? "normal";
			switch (verbosityValue)
			{
				case "q":
				case "quiet":
					verbosity.MinimumLevel = LogEventLevel.Fatal + 1;
					break;
				case "m":
				case "minimal":
					verbosity.MinimumLevel = LogEventLevel.Warning;
					break;
				case "n":
				case "normal":
					verbosity.MinimumLevel = LogEventLevel.Information;
					break;
				case "d":
				case "detailed":
					verbosity.MinimumLevel = LogEventLevel.Debug;
					break;
				// ReSharper disable once StringLiteralTypo
				case "diag":
				case "diagnostic":
					verbosity.MinimumLevel = LogEventLevel.Verbose;
					break;
				default:
					throw new CommandParsingException($"Unknown verbosity level '{verbosityValue}'.", result.Command().HelpView().TrimEnd());
			}

			Log.Verbose(result.Diagram());

			var items = command.Value<string[]>();

			var conversionOptions = new ConversionOptions
			{
				ProjectCache = new DefaultProjectCache(),
				Force = command.ValueOrDefault<bool>("force"),
				KeepAssemblyInfo = command.ValueOrDefault<bool>("keep-assembly-info")
			};

			switch (command.Name)
			{
				case "evaluate":
				case "migrate":
					var frameworks = command.ValueOrDefault<string[]>("target-frameworks");
					if (frameworks != null)
						conversionOptions.TargetFrameworks = frameworks;
					break;
			}

			var logic = new CommandLogic();
			switch (command.Name)
			{
				case "wizard":
					logic.ExecuteWizard(items, conversionOptions);
					break;
				case "evaluate":
					logic.ExecuteEvaluate(items, conversionOptions);
					break;
				case "analyze":
					logic.ExecuteAnalyze(items, conversionOptions);
					break;
				case "migrate":
					conversionOptions.AppendTargetFrameworkToOutputPath = !command.ValueOrDefault<bool>("old-output-path");

					var forceTransformations = command.ValueOrDefault<string[]>("force-transformations");
					if (forceTransformations != null)
						conversionOptions.ForceDefaultTransforms = forceTransformations;

					logic.ExecuteMigrate(items, command.ValueOrDefault<bool>("no-backup"), command.ValueOrDefault<bool>("cleanup"), conversionOptions);
					break;
			}

			return result.Execute().Code;
		}

		private static readonly Parser Instance = new Parser(options: RootCommand());

		private static ArgumentsRule DefaultToCurrentDirectory(this ArgumentsRule rule) =>
			rule.With(defaultValue: () => NuGet.Common.PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory()));

		private static Command RootCommand() =>
			Command("dotnet-migrate-2017",
				".NET Project Migration Tool",
				NoArguments(),
				Wizard(),
				Evaluate(),
				Migrate(),
				Analyze(),
				HelpOption(),
				VerbosityOption());

		private static ArgumentsRule ItemsArgument => ZeroOrMoreArguments()
			.With("Project/solution file paths or glob patterns", "items")
			.DefaultToCurrentDirectory();

		private static Option TargetFrameworksOption => Option(
			"-t|--target-frameworks",
			"Override project target frameworks with ones specified. Specify multiple times for multiple target frameworks.",
			OneOrMoreArguments()
				.With("Target frameworks to be used instead of the ones in source projects", "frameworks"));

		private static Option KeepAssemblyInfoOption => Option("-a|--keep-assembly-info",
			"Keep assembly attributes in AssemblyInfo file instead of moving them to project file.");

		private static Option ForceOption => Option("-f|--force",
			"Force a conversion even if not all preconditions are met.");

		private static Command Evaluate() =>
			Command("evaluate",
				"Examine the projects potential to be converted before actual migration",
				ItemsArgument,
				TargetFrameworksOption,
				HelpOption());

		private static Command Migrate() =>
			Command("migrate",
				"Migrate projects to VS2017+ CPS format (non-interactive)",
				ItemsArgument,
				Option("-n|--no-backup",
					"Skip moving project.json, global.json, and *.xproj to a `Backup` directory after successful migration."),
				ForceOption,
				KeepAssemblyInfoOption,
				TargetFrameworksOption,
				Option("-o|--old-output-path",
					"Preserve legacy behavior by not creating a subfolder with the target framework in the output path."),
				Option(
					"-ft|--force-transformations",
					"Force execution of transformations despite project conversion state by their specified names. " +
					"Specify multiple times for multiple enforced transformations.",
					OneOrMoreArguments()
						.With("Transformation names to enforce execution", "names")),
				Option("-c|--cleanup", "Cleanup package references, if transitive references are found"),
				HelpOption());

		private static Command Analyze() =>
			Command("analyze",
				"Do the analysis run and output diagnostics",
				ItemsArgument,
				HelpOption());

		private static Command Wizard() =>
			Command("wizard",
				"Launch interactive migration wizard (recommended)",
				ItemsArgument,
				ForceOption,
				KeepAssemblyInfoOption,
				HelpOption());

		private static Option HelpOption() =>
			Option("-h|--help",
				"Show help information",
				NoArguments());

		private static Option VerbosityOption() =>
			Option("-v|--verbosity",
				// ReSharper disable StringLiteralTypo
				"Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]",
				// ReSharper restore StringLiteralTypo
				AnyOneOf("q", "quiet",
						"m", "minimal",
						"n", "normal",
						"d", "detailed",
						// ReSharper disable once StringLiteralTypo
						"diag", "diagnostic")
					.With(name: "LEVEL"));

		public static void ShowHelpOrErrorIfAppropriate(this ParseResult parseResult)
		{
			parseResult.ShowHelpIfRequested();

			if (parseResult.Errors.Any())
			{
				throw new CommandParsingException(
					string.Join(Environment.NewLine,
						parseResult.Errors.Select(e => e.Message)),
					parseResult.Command()?.HelpView().TrimEnd());
			}
		}

		private static void ShowHelpIfRequested(this ParseResult parseResult)
		{
			var appliedCommand = parseResult.AppliedCommand();

			if (appliedCommand.HasOption("help") ||
				appliedCommand.Arguments.Contains("-?") ||
				appliedCommand.Arguments.Contains("/?"))
			{
				throw new HelpException(parseResult.Command().HelpView().TrimEnd());
			}
		}

		public static T ValueOrDefault<T>(this AppliedOption parseResult, string alias)
		{
			return parseResult
				.AppliedOptions
				.Where(o => o.HasAlias(alias))
				.Select(o => o.Value<T>())
				.SingleOrDefault();
		}

		/// <summary>Allows control flow to be interrupted in order to display help in the console.</summary>
		public sealed class HelpException : Exception
		{
			public HelpException(string message) : base(message)
			{
			}
		}

		private sealed class CommandParsingException : Exception
		{
			public CommandParsingException(
				string message,
				string helpText = null) : base(message)
			{
				HelpText = helpText ?? "";
			}

			public string HelpText { get; }
		}
	}
}