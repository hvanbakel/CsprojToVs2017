using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Project2015To2017.Tests")]

namespace Project2015To2017
{
	public sealed class ProjectConverter
	{
		public static readonly IReadOnlyDictionary<string, string> ProjectFileMappings = new Dictionary<string, string>
		{
			{ ".csproj", "cs" },
			{ ".vbproj", "vb" },
			{ ".fsproj", "fs" }
		};

		private readonly ILogger logger;
		private readonly ConversionOptions conversionOptions;
		private readonly ProjectReader projectReader;
		private readonly ITransformationSet transformationSet;

		public ProjectConverter(
			ILogger logger,
			ITransformationSet transformationSet,
			ConversionOptions conversionOptions = null)
		{
			this.logger = logger;
			this.conversionOptions = conversionOptions ?? new ConversionOptions();
			this.transformationSet = transformationSet ?? BasicReadTransformationSet.Instance;
			projectReader = new ProjectReader(logger, this.conversionOptions);
		}

		public IEnumerable<Project> ProcessSolutionFile(Solution solution)
		{
			logger.LogTrace("Solution parsing started.");
			if (solution.ProjectPaths == null)
			{
				logger.LogTrace($"'{nameof(solution.ProjectPaths)}' is null");
				yield break;
			}

			foreach (var projectReference in solution.ProjectPaths)
			{
				logger.LogDebug("Project found: " + projectReference.Include);
				if (!projectReference.ProjectFile.Exists)
				{
					logger.LogError("Project file not found at: " + projectReference.ProjectFile.FullName);
					continue;
				}

				yield return ProcessProjectFile(projectReference.ProjectFile, solution, projectReference);
			}
		}

		public Project ProcessProjectFile(FileInfo file, Solution solution, ProjectReference reference = null)
		{
			if (!Validate(file, logger))
			{
				return null;
			}

			var project = projectReader.Read(file);
			if (project == null)
			{
				return null;
			}

			if (reference?.ProjectName != null)
			{
				project.ProjectName = reference.ProjectName;
			}

			if (!project.Valid)
			{
				logger.LogWarning("Project {ProjectName} is marked as invalid, skipping...", project.ProjectName ?? project.FilePath.Name);
				return null;
			}

			project.CodeFileExtension = ProjectFileMappings[file.Extension];
			project.Solution = solution;

			foreach (var transform in conversionOptions.PreDefaultTransforms)
			{
				transform.Transform(project);
			}

			foreach (var transform in transformationSet.IterateSuitableTransformations(project, logger, conversionOptions))
			{
				transform.Transform(project);
			}

			foreach (var transform in conversionOptions.PostDefaultTransforms)
			{
				transform.Transform(project);
			}

			return project;
		}

		internal static bool Validate(FileInfo file, ILogger logger)
		{
			if (file.Exists)
			{
				return true;
			}

			logger.LogError($"File {file.FullName} could not be found.");
			return false;
		}
	}
}
