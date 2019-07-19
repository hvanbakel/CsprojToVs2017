using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using NuGet.Common;
using Serilog;
using Serilog.Core;

namespace Project2015To2017.Migrate2017.Tool
{
	internal static partial class ProgramBase
	{
		internal static readonly LoggingLevelSwitch verbosity = new LoggingLevelSwitch();

		private static ArgumentsRule DefaultToCurrentDirectory(this ArgumentsRule rule) =>
			rule.With(defaultValue: () => PathUtility.EnsureTrailingSlash(Directory.GetCurrentDirectory()));

		internal static Option ForceOption => Create.Option("-f|--force",
			"Force a conversion even if not all preconditions are met.");

		internal static Option HelpOption() =>
			Create.Option("-h|--help",
				"Show help information",
				Accept.NoArguments());

		internal static Option VerbosityOption() =>
			Create.Option("-v|--verbosity",
				// ReSharper disable StringLiteralTypo
				"Set the verbosity level of the command. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]",
				// ReSharper restore StringLiteralTypo
				Accept.AnyOneOf("q", "quiet",
						"m", "minimal",
						"n", "normal",
						"d", "detailed",
						// ReSharper disable once StringLiteralTypo
						"diag", "diagnostic")
					.With(name: "LEVEL"));

		internal static void ShowHelpOrErrorIfAppropriate(this ParseResult parseResult)
		{
			parseResult.ShowHelpIfRequested();

			if (parseResult.Errors.Any())
			{
				throw new CommandParsingException(
					String.Join(Environment.NewLine,
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

		internal static void CreateLogger()
		{
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithDemystifiedStackTraces()
				.MinimumLevel.ControlledBy(ProgramBase.verbosity)
				.WriteTo.Console()
				.CreateLogger();
		}
	}
}