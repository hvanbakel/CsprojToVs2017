using Project2015To2017.Analysis.Diagnostics;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project2015To2017.Analysis
{
	public class Analyzer
	{
		/// <summary>
		/// All user definable analysis properties to use during the process
		/// </summary>
		public AnalysisOptions Options { get; }

		/// <summary>
		/// User reporter to use for output
		/// </summary>
		public IReporter Reporter { get; }

		public Analyzer(AnalysisOptions options = null, IReporter reporter = null)
		{
			Options = options ?? new AnalysisOptions();
			Reporter = reporter ?? new ConsoleReporter(options);

			if (reporter != null && reporter is ReporterBase reporterBase)
			{
				reporterBase.Options = Options;
			}

			foreach (var diagnostic in Options.Diagnostics)
			{
				diagnostic.Options = Options;
				diagnostic.Reporter = Reporter;
			}
		}

		public void Analyze(Project project)
		{
			if (project == null)
			{
				throw new ArgumentNullException(nameof(project));
			}

			if (Options.RootDirectory == null)
			{
				Options.RootDirectory = project.Solution?.FilePath.Directory ?? project.FilePath.Directory;
			}

			foreach (var diagnostic in Options.Diagnostics)
			{
				if (diagnostic.SkipForModernProject && project.IsModernProject)
				{
					continue;
				}

				if (diagnostic.SkipForLegacyProject && !project.IsModernProject)
				{
					continue;
				}

				diagnostic.Analyze(project);
			}
		}

		public void Analyze(Solution solution)
		{
			if (solution == null)
			{
				throw new ArgumentNullException(nameof(solution));
			}

			if (Options.RootDirectory == null)
			{
				Options.RootDirectory = solution.FilePath.Directory;
			}

			if (solution.ProjectPaths == null)
			{
				return;
			}

			foreach (var projectPath in solution.ProjectPaths)
			{
				if (!projectPath.ProjectFile.Exists)
				{
					Reporter.Report("W002",
						$"Referenced project file '{projectPath.Include}' was not found at '{projectPath.ProjectFile.FullName}'.",
						solution.FilePath);
					continue;
				}

				var project = new ProjectReader(projectPath.ProjectFile).Read();

				Analyze(project);
			}
		}
	}
}