using Microsoft.DotNet.Cli.CommandLine;
using Serilog.Events;

namespace Project2015To2017.Migrate2017.Tool
{
	internal static partial class ProgramBase
	{
		public static void ApplyVerbosity(ParseResult result, AppliedOption globalOptions)
		{
			var verbosityValue = globalOptions.ValueOrDefault<string>("verbosity")?.Trim().ToLowerInvariant() ??
			                     "normal";
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
					throw new CommandParsingException($"Unknown verbosity level '{verbosityValue}'.",
						result.Command().HelpView().TrimEnd());
			}
		}
	}
}