using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommandLine;

namespace Project2015To2017.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
				.WithParsed(ConvertProject);

		}
		private static void ConvertProject(Options options)
		{
#if DEBUG
			var progress = new Progress<string>(x => Debug.WriteLine(x));
#else
			var progress = new Progress<string>(System.Console.WriteLine);
#endif

			var convertedProjects = new List<Definition.Project>();
			foreach (string file in options.Files)
			{
				convertedProjects.AddRange(ProjectConverter.Convert(file, progress)
															.Where(x => x != null)
															.ToList());
			}

			//change options
			foreach (var project in convertedProjects)
			{
				options.UpdateProject(project);
			}

			if (!options.DryRun)
			{
				var doBackup = !options.NoBackup;

				var writer = new Writing.ProjectWriter(x => x.Delete(), _ => { });
				foreach (var project in convertedProjects)
				{
					writer.Write(project, doBackup, progress);
				}
			}
		}

	}
}
