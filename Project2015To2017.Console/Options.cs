using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace Project2015To2017.Console
{
	public class Options
	{
		[Value(0)]
		public IEnumerable<string> Files { get; set; }

		[Option('d', "dry-run", Default = false, HelpText = "Will not update any files, just outputs all the messages")]
		public bool DryRun { get; set; } = false;

		[Option('n', "no-backup", Default = false, HelpText = "Will not create a backup folder")]
		public bool NoBackup { get; set; } = false;

		[Option('a', "assembly-info", Default = false, HelpText = "Keep Assembly Info in a file")]
		public bool AssemblyInfo { get; set; } = false;

		[Option('t', "target-frameworks", Separator = ';', HelpText = "Specific target frameworks")]
		public IEnumerable<string> TargetFrameworks { get; set; }

		[Option('o', "output-path", Default = false, HelpText = "Will not create a subfolder with the target framework in the output path")]
		public bool NoTargetFrameworkToOutputPath { get; set; } = false;

		[Option('f', "force", Default = false, HelpText = "Will force an upgrade even though certain preconditions might not have been met")]
		public bool Force { get; set; } = false;

		public ConversionOptions ConversionOptions
			=> new ConversionOptions
			{
				KeepAssemblyInfo = AssemblyInfo,
				TargetFrameworks = TargetFrameworks?.ToList(),
				AppendTargetFrameworkToOutputPath = !NoTargetFrameworkToOutputPath,
				ProjectCache = new Caching.DefaultProjectCache(),
				Force = this.Force
			};
	}
}