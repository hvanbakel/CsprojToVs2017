using System.Collections.Generic;
using System.Linq;
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

			System.Console.Out.Flush();
		}

		private static void ConvertProject(Options options)
		{
			Log("csproj-to-2017 is obsolete. Use Project2015To2017.Migrate2017.Tool (dotnet migrate-2017).");
			Log("> dotnet tool install --global Project2015To2017.Migrate2017.Tool");

			if (options.TargetFrameworks.ToArray().Length > 0)
			{
				Log("Enforcing target frameworks is likely not what you need.");
				Log("Try using interactive wizard, it can detect such cases and handle them better.");
				Log("> dotnet migrate-2017 wizard -h");
				Log("Or if you know better, migrate command still accepts this argument.");
				return;
			}

			if (options.DryRun)
			{
				Log("Conversion evaluation run can be done with evaluate command.");
				Log("> dotnet migrate-2017 evaluate -h");
				return;
			}

			if (options.NoTargetFrameworkToOutputPath)
			{
				Log("Enforcing non-default behavior is not recommended.");
				Log("Modernization can be performed while following interactive wizard.");
				Log("> dotnet migrate-2017 wizard -h");
				return;
			}

			if (options.NoBackup)
			{
				Log("Interactive wizard will ask you whether to create backups at each conversion stage.");
				Log("Therefore -n is not needed anymore for wizard command.");
				Log("> dotnet migrate-2017 wizard -h");
				return;
			}

			var filesArray = options.Files.ToArray();

			var optionSet = new List<string>
			{
				"dotnet", "migrate-2017", "wizard"
			};

			if (options.Force)
				optionSet.Add("-f");
			if (options.AssemblyInfo)
				optionSet.Add("-a");

			if (filesArray.Length > 0)
				optionSet.AddRange(filesArray.Select(WrapPathIfNecessary));

			Log($"> {string.Join(" ", optionSet)}");
		}

		private static string WrapPathIfNecessary(string arg)
		{
			return arg.Contains(' ') ? $"\"{arg}\"" : arg;
		}

		private static void Log(string value) => System.Console.WriteLine(value);
	}
}