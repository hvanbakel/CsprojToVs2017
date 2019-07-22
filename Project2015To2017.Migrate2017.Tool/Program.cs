using System;
using Microsoft.DotNet.Cli.CommandLine;
using Project2015To2017.Analysis;
using Project2015To2017.Caching;
using Project2015To2017.Transforms;
using Serilog;
using Serilog.Events;
using static Microsoft.DotNet.Cli.CommandLine.Accept;
using static Microsoft.DotNet.Cli.CommandLine.Create;

namespace Project2015To2017.Migrate2017.Tool
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			ProgramBase.CreateLogger();

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

		private static int ProcessArgs(ParseResult result)
		{
			result.ShowHelpOrErrorIfAppropriate();

			var command = result.AppliedCommand();
			var globalOptions = result["dotnet-migrate-2017"];
			ProgramBase.ApplyVerbosity(result, globalOptions);

			Log.Verbose(result.Diagram());

			var items = command.Value<string[]>();

			var conversionOptions = new ConversionOptions
			{
				ProjectCache = new DefaultProjectCache(),
				ForceOnUnsupportedProjects = command.ValueOrDefault<bool>("force"),
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
					var diagnostics = new DiagnosticSet(Vs15DiagnosticSet.All);
					diagnostics.ExceptWith(DiagnosticSet.All);
					var sets = new WizardTransformationSets
					{
						MigrateSet = new ChainTransformationSet(
							new BasicSimplifyTransformationSet(Vs15TransformationSet.TargetVisualStudioVersion),
							Vs15TransformationSet.TrueInstance),
						ModernCleanUpSet = new BasicSimplifyTransformationSet(
							Vs15TransformationSet.TargetVisualStudioVersion),
						ModernizeSet = new ChainTransformationSet(
							new BasicSimplifyTransformationSet(Vs15TransformationSet.TargetVisualStudioVersion),
							Vs15ModernizationTransformationSet.TrueInstance),
						Diagnostics = diagnostics
					};

					logic.ExecuteWizard(items, conversionOptions, sets);
					break;
				case "evaluate":
					logic.ExecuteEvaluate(items, conversionOptions, Vs15TransformationSet.Instance, new AnalysisOptions(Vs15DiagnosticSet.All));
					break;
				case "analyze":
					logic.ExecuteAnalyze(items, conversionOptions, new AnalysisOptions(Vs15DiagnosticSet.All));
					break;
				case "migrate":
					conversionOptions.AppendTargetFrameworkToOutputPath = !command.ValueOrDefault<bool>("old-output-path");

					var forceTransformations = command.ValueOrDefault<string[]>("force-transformations");
					if (forceTransformations != null)
						conversionOptions.ForceDefaultTransforms = forceTransformations;

					logic.ExecuteMigrate(items, command.ValueOrDefault<bool>("no-backup"), conversionOptions, Vs15TransformationSet.Instance);
					break;
			}

			return result.Execute().Code;
		}

		private static Command RootCommand() =>
			Command("dotnet-migrate-2017",
				".NET Project Migration Tool",
				NoArguments(),
				ProgramBase.Wizard(),
				ProgramBase.Evaluate(),
				ProgramBase.Migrate(),
				ProgramBase.Analyze(),
				ProgramBase.HelpOption(),
				ProgramBase.VerbosityOption());

		private static readonly Parser Instance = new Parser(options: RootCommand());
	}
}