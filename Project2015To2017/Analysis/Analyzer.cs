using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;

namespace Project2015To2017.Analysis
{
	public class Analyzer
	{
		private readonly AnalysisOptions _options;
		private readonly IReporter _reporter;

		public Analyzer(AnalysisOptions options = null, IReporter reporter = null)
		{
			_options = options ?? new AnalysisOptions();
			_reporter = reporter ?? new ConsoleReporter();
		}

		public void Analyze(Project project)
		{
			if (project == null)
			{
				throw new ArgumentNullException(nameof(project));
			}

			foreach (var diagnostic in _options.Diagnostics)
			{
				if (diagnostic.SkipForModernProject && project.IsModernProject)
				{
					continue;
				}

				if (diagnostic.SkipForLegacyProject && !project.IsModernProject)
				{
					continue;
				}

				_reporter.Report(diagnostic.Analyze(project), new ReporterOptions
				{
					RootDirectory = project.TryFindBestRootDirectory()
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
					_reporter.Report(new[]
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