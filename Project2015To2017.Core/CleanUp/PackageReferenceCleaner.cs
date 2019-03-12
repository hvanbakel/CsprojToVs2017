using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NuGet.ProjectModel;
using Project2015To2017.Definition;

namespace Project2015To2017.CleanUp
{
	public class PackageReferenceCleaner
	{
		private readonly ILogger _logger;

		public PackageReferenceCleaner(ILogger logger)
		{
			_logger = logger;
		}

		public bool CleanUpProjectReferences(Project project)
		{
			var graphService = new DependencyGraphService();
			var graph = graphService.GenerateDependencyGraph(project.FilePath);
			var changed = false;

			foreach (var packageSpec in graph.Projects.Where(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference))
			{
				foreach (var removableProjectReference in GetRemovableProjectReferences(packageSpec))
				{
					var packageReferences = project.PackageReferences.Where(pr => pr.Id != removableProjectReference.Name).ToList();

					_logger.LogInformation($"The following references are removed: {string.Join(Environment.NewLine, removableProjectReference)}");

					project.PackageReferences = new ReadOnlyCollection<PackageReference>(packageReferences);
					changed = true;
				}
			}

			return changed;
		}

		private IEnumerable<Dependency> GetRemovableProjectReferences(PackageSpec packageSpec)
		{
			var lockFile = LockFileService.GetLockFile(packageSpec.FilePath, packageSpec.RestoreMetadata.OutputPath ?? Path.GetTempPath());
			var collection = new List<Dependency>();

			foreach (var targetFramework in packageSpec.TargetFrameworks)
			{
				var dependencies = GetDependencies(lockFile, targetFramework);
				collection.AddRange(GetRemovableDependencies(dependencies));
			}

			return collection;
		}

		private IEnumerable<Dependency> GetRemovableDependencies(IEnumerable<Dependency> dependencies)
		{
			var childrenDependencies = dependencies.SelectMany(dep => dep.Children).ToList();

			foreach (var dependency in dependencies)
			{
				var children = childrenDependencies.Where(c => c.Equals(dependency)).ToList();
				if (children.Count > 0)
				{
					dependency.ContainingPackages = children;
					yield return dependency;
				}
			}
		}

		private IEnumerable<Dependency> GetDependencies(LockFile lockFile, TargetFrameworkInformation targetFramework)
		{
			var dependencies = new List<Dependency>();
			var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));

			if (lockFileTargetFramework != null)
			{
				foreach (var dependency in targetFramework.Dependencies)
				{
					var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == dependency.Name);
					var reportDependency = ReportDependency(projectLibrary, lockFileTargetFramework, 1);

					if (reportDependency == null) continue;

					dependencies.Add(reportDependency);
				}
			}

			return dependencies;
		}

		private Dependency ReportDependency(LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework, int indentLevel, Dependency dependency = null)
		{
			if (projectLibrary == null)
				return null;

			if (indentLevel == 1)
				dependency = new Dependency(projectLibrary.Name, projectLibrary.Version.OriginalVersion);

			foreach (var childDependency in projectLibrary.Dependencies)
			{
				var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == childDependency.Id);
				dependency.Children.Add(new Dependency(childDependency.Id, childDependency.VersionRange.MinVersion.OriginalVersion) { Parent = dependency.Name });
				ReportDependency(childLibrary, lockFileTargetFramework, indentLevel + 1, dependency);
			}

			return dependency;
		}
	}
}