using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;

namespace Project2015To2017.Analysis
{
	public class Analyzer<TReporter, TReporterOptions>
		where TReporter : class, IReporter<TReporterOptions>
		where TReporterOptions : IReporterOptions
	{
		private readonly AnalysisOptions _options;
		private readonly TReporter _reporter;

		public Analyzer(TReporter reporter, AnalysisOptions options = null)
		{
			this._reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
			this._options = options ?? new AnalysisOptions();
		}

		public void Analyze(Project project)
		{
			if (project == null)
			{
				throw new ArgumentNullException(nameof(project));
			}

			foreach (var diagnostic in this._options.Diagnostics)
			{
				if (diagnostic.SkipForModernProject && project.IsModernProject)
				{
					continue;
				}

				if (diagnostic.SkipForLegacyProject && !project.IsModernProject)
				{
					continue;
				}

				var reporterOptions = this._reporter.CreateOptionsForProject(project);
				this._reporter.Report(diagnostic.Analyze(project), reporterOptions);
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

			var projectReader = new ProjectReader(NoopLogger.Instance);
			foreach (var projectPath in solution.ProjectPaths)
			{
				if (!projectPath.ProjectFile.Exists)
				{
					this._reporter.Report(new[]
					{
						new DiagnosticResult
						{
							Code = "W002",
							Message =
								$"Referenced project file '{projectPath.Include}' was not found at '{projectPath.ProjectFile.FullName}'.",
							Location = new DiagnosticLocation
							{
								Source = solution.FilePath
							}
						}
					}, this._reporter.DefaultOptions);
					continue;
				}

				var project = projectReader.Read(projectPath.ProjectFile);

				Analyze(project);
			}
		}
	}
}