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

		public ConversionOptions ConversionOptions
			=> new ConversionOptions
			{
				KeepAssemblyInfo = AssemblyInfo,
				TargetFrameworks = TargetFrameworks?.ToList()
			};
	}
}