using Microsoft.DotNet.Cli.CommandLine;

namespace Project2015To2017.Migrate2017.Tool
{
	internal static partial class ProgramBase
	{
		internal static Command Evaluate() =>
			Create.Command("evaluate",
				"Examine the projects potential to be converted before actual migration",
				ItemsArgument,
				TargetFrameworksOption,
				HelpOption());

		internal static Command Migrate() =>
			Create.Command("migrate",
				"Migrate projects to modern Visual Studio CPS format (non-interactive)",
				ItemsArgument,
				Create.Option("-n|--no-backup",
					"Skip moving project.json, global.json, and *.xproj to a `Backup` directory after successful migration."),
				ForceOption,
				KeepAssemblyInfoOption,
				TargetFrameworksOption,
				Create.Option("-o|--old-output-path",
					"Preserve legacy behavior by not creating a subfolder with the target framework in the output path."),
				Create.Option(
					"-ft|--force-transformations",
					"Force execution of transformations despite project conversion state by their specified names. " +
					"Specify multiple times for multiple enforced transformations.",
					Accept.OneOrMoreArguments()
						.With("Transformation names to enforce execution", "names")),
				HelpOption());

		internal static Command Analyze() =>
			Create.Command("analyze",
				"Do the analysis run and output diagnostics",
				ItemsArgument,
				HelpOption());

		internal static Command Wizard() =>
			Create.Command("wizard",
				"Launch interactive migration wizard (recommended)",
				ItemsArgument,
				ForceOption,
				KeepAssemblyInfoOption,
				HelpOption());
	}
}