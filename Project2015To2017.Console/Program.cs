using System;
using System.Diagnostics;
using System.Linq;

namespace Project2015To2017.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				System.Console.WriteLine("Please specify a project file.");
				return;
			}


#if DEBUG
			var progress = new Progress<string>(x => Debug.WriteLine(x));
#else
			var progress = new Progress<string>(System.Console.WriteLine);
#endif

			var convertedProjects = ProjectConverter.Convert(args[0], progress)
													.Where(x => x != null)
													.ToList();

			if (!args.Contains("--dry-run"))
			{
				var doBackup = !args.Contains("--no-backup");

				var writer = new Writing.ProjectWriter(x => x.Delete(), _ => { });
				foreach (var project in convertedProjects)
				{
					writer.Write(project, doBackup, progress);
				}
			}
		}
	}
}
