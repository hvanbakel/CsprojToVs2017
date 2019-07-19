using Microsoft.DotNet.Cli.CommandLine;

namespace Project2015To2017.Migrate2017.Tool
{
	internal static partial class ProgramBase
	{
		private static ArgumentsRule ItemsArgument => Accept.ZeroOrMoreArguments()
			.With("Project/solution file paths or glob patterns", "items")
			.DefaultToCurrentDirectory();

		private static Option TargetFrameworksOption => Create.Option(
			"-t|--target-frameworks",
			"Override project target frameworks with ones specified. Specify multiple times for multiple target frameworks.",
			Accept.OneOrMoreArguments()
				.With("Target frameworks to be used instead of the ones in source projects", "frameworks"));

		private static Option KeepAssemblyInfoOption => Create.Option("-a|--keep-assembly-info",
			"Keep assembly attributes in AssemblyInfo file instead of moving them to project file.");
	}
}