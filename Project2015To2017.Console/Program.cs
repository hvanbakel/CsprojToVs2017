using CommandLine;
using Microsoft.Extensions.Logging;

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
			ILogger logger = new ConsoleLogger("console", (s, l) => l >= LogLevel.Information, true);

			logger.LogError("csproj-to-2017 is obsolete. Use Project2015To2017.Migrate2017.Tool (dotnet migrate-2017).");
		}
	}
}