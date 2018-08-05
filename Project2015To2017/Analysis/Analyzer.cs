using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;

namespace Project2015To2017.Analysis
{
	public class Analyzer
	{
		private readonly AnalysisOptions options;
		private readonly IReporter reporter;

		public Analyzer(AnalysisOptions options = null, IReporter reporter = null)
		{
			this.options = options ?? new AnalysisOptions();
			this.reporter = reporter ?? new ConsoleReporter();
		}

		public void Analyze(Project project)
		{
			if (project == null)
			{
				throw new ArgumentNullException(nameof(project));
			}

			foreach (var diagnostic in this.options.Diagnostics)
			{
				if (diagnostic.SkipForModernProject && project.IsModernProject)
				{
					continue;
				}

				if (diagnostic.SkipForLegacyProject && !project.IsModernProject)
				{
					continue;
				}

				this.reporter.Report(diagnostic.Analyze(project), new ReporterOptions
				{
					RootDirectory = project.Solution?.FilePath.Directory ?? project.FilePath.Directory
				});
			}
		}

		public void Analyze(Solution solution)
		{
			if (solution == null)
			{
				throw new ArgumentNullException(nameof(solution));
			}

			if (solution.ProjectPaths == null)
			{
				return;
			}

			var reporterOptions = new ReporterOptions
			{
				RootDirectory = solution.FilePath.Directory
			};

			foreach (var projectPath in solution.ProjectPaths)
			{
				if (!projectPath.ProjectFile.Exists)
				{
					this.reporter.Report(new[]
					{
						new DiagnosticResult
						{
							Code = "W002",
							Message = $"Referenced project file '{projectPath.Include}' was not found at '{projectPath.ProjectFile.FullName}'.",
							Location = new DiagnosticLocation
							{
								Source = solution.FilePath
							}
						}
					}, reporterOptions);
					continue;
				}

				var project = new ProjectReader(projectPath.ProjectFile).Read();

				Analyze(project);
			}
		}
	}
}