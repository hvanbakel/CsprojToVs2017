using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public sealed class SolutionReader
	{
		public static readonly SolutionReader Instance = new SolutionReader();

		// An example of a project line looks like this:
		//  Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ClassLibrary1", "ClassLibrary1\ClassLibrary1.csproj", "{05A5AD00-71B5-4612-AF2F-9EA9121C4111}"
		private static readonly Lazy<Regex> crackProjectLine = new Lazy<Regex>(
			() => new Regex
			(
				"^" // Beginning of line
				+ "Project\\(\"(?<PROJECTTYPEGUID>.*)\"\\)"
				+ "\\s*=\\s*" // Any amount of whitespace plus "=" plus any amount of whitespace
				+ "\"(?<PROJECTNAME>.*)\""
				+ "\\s*,\\s*" // Any amount of whitespace plus "," plus any amount of whitespace
				+ "\"(?<RELATIVEPATH>.*)\""
				+ "\\s*,\\s*" // Any amount of whitespace plus "," plus any amount of whitespace
				+ "\"(?<PROJECTGUID>.*)\""
				+ "$", // End-of-line
				RegexOptions.Compiled
			)
		);

		private const string vbProjectGuid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
		private const string csProjectGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
		private const string cpsProjectGuid = "{13B669BE-BB05-4DDF-9536-439F39A36129}";
		private const string cpsCsProjectGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
		private const string cpsVbProjectGuid = "{778DAE3C-4631-46EA-AA77-85C1314464D9}";
		private const string fsProjectGuid = "{F2A71F9B-5D33-465A-A702-920D77279786}";
		private const string solutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

		private SolutionReader()
		{
		}

		public Solution Read(string filePath, ILogger logger = null)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));
			}

			logger = logger ?? NoopLogger.Instance;
			return Read(new FileInfo(filePath), logger);
		}

		public Solution Read(FileInfo fileInfo, ILogger logger)
		{
			if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

			var unsupported = new List<string>();
			var projectPaths = new List<ProjectReference>();
			using (var reader = new StreamReader(fileInfo.OpenRead()))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					line = line.Trim();
					if (!line.StartsWith("Project(", StringComparison.Ordinal))
					{
						continue;
					}

					if (!ParseFirstProjectLine(line,
						out var projectTypeGuid, out var projectName, out var path, out var projectGuid))
					{
						if (projectTypeGuid != solutionFolderGuid)
						{
							logger.LogWarning("Unsupported project[{Name}] type {Type}", projectName, projectTypeGuid);
							unsupported.Add(path);
						}

						continue;
					}

					var adjustedPath = Extensions.MaybeAdjustFilePath(path, fileInfo.DirectoryName);
					var reference = new ProjectReference
					{
						Include = path,
						ProjectFile = new FileInfo(Path.Combine(fileInfo.DirectoryName, adjustedPath)),
						ProjectGuid = Guid.Parse(projectGuid),
						ProjectTypeGuid = projectTypeGuid,
						ProjectName = projectName
					};

					projectPaths.Add(reference);
				}
			}

			var solutionDefinition = new Solution
			{
				FilePath = fileInfo,
				ProjectPaths = projectPaths,
				UnsupportedProjectPaths = unsupported,
			};

			return solutionDefinition;
		}

		/// <summary>
		/// Parse the first line of a Project section of a solution file. This line should look like:
		///
		///  Project("{Project type GUID}") = "Project name", "Relative path to project file", "{Project GUID}"
		///
		/// </summary>
		/// <returns>true if project type is supported by csproj-to-2017</returns>
		private bool ParseFirstProjectLine(string firstLine,
			out string projectTypeGuid,
			out string projectName,
			out string path,
			out string projectGuid)
		{
			var match = crackProjectLine.Value.Match(firstLine);
			if (!match.Success)
			{
				throw new ArgumentException("Invalid Project definition line format", nameof(firstLine));
			}

			projectTypeGuid = match.Groups["PROJECTTYPEGUID"].Value.Trim();
			projectName = match.Groups["PROJECTNAME"].Value.Trim();
			path = match.Groups["RELATIVEPATH"].Value.Trim();
			projectGuid = match.Groups["PROJECTGUID"].Value.Trim();

			// If the project name is empty (as in some bad solutions) set it to some generated generic value.
			// This allows us to at least generate reasonable target names etc. instead of crashing.
			if (string.IsNullOrEmpty(projectName))
			{
				projectName = "EmptyProjectName." + Guid.NewGuid();
			}

			return (string.Compare(projectTypeGuid, vbProjectGuid, StringComparison.OrdinalIgnoreCase) == 0) ||
			       (string.Compare(projectTypeGuid, csProjectGuid, StringComparison.OrdinalIgnoreCase) == 0) ||
			       (string.Compare(projectTypeGuid, cpsProjectGuid, StringComparison.OrdinalIgnoreCase) == 0) ||
			       (string.Compare(projectTypeGuid, cpsCsProjectGuid, StringComparison.OrdinalIgnoreCase) == 0) ||
			       (string.Compare(projectTypeGuid, cpsVbProjectGuid, StringComparison.OrdinalIgnoreCase) == 0) ||
			       (string.Compare(projectTypeGuid, fsProjectGuid, StringComparison.OrdinalIgnoreCase) == 0);
		}
	}
}