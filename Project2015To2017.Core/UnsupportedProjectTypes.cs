using System;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017
{
	/// <summary>
	/// Helper library to filter out unsupported project types
	/// </summary>
	public static class UnsupportedProjectTypes
	{
		/// <summary>
		/// Check for unsupported ProjectTypeGuids in project
		/// </summary>
		/// <param name="project">source project to check</param>
		/// <returns></returns>
		public static UnsupportedProjectReason IsUnsupportedProjectType(Project project)
		{
			if (project == null) throw new ArgumentNullException(nameof(project));

			if (project.ProjectFolder != null)
				if (CheckForEntityFramework(project))
					return UnsupportedProjectReason.EntityFramework;

			var guidTypes = project.IterateProjectTypeGuids();

			// if any guid matches an unsupported type, return true
			return guidTypes.Any(t => unsupportedGuids.Contains(t.guid)) ? UnsupportedProjectReason.NotSupportedProjectType : UnsupportedProjectReason.Supported;
		}

		private static bool CheckForEntityFramework(Project project)
		{
			// Code-first
			if (project.ProjectFolder.EnumerateDirectories().Any(x =>
				string.Equals(x.Name, "Migrations", StringComparison.OrdinalIgnoreCase)))
			{
				return true;
			}

			// EF Designer
			if (project.FindAllWildcardFiles("edmx").Any())
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Guids that cannot be converted
		/// </summary>
		/// <remarks>
		/// Types of projects that are not supported:
		/// https://github.com/dotnet/project-system/blob/master/docs/feature-comparison.md
		/// The GUIDs taken from
		/// https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
		/// Note that the list here is in upper case but project file guids are normally lower case
		/// This list does not include Windows Forms apps, these have no type guid
		/// </remarks>
		private static readonly Guid[] unsupportedGuids =
		{
			Guid.ParseExact("8BB2217D-0F2D-49D1-97BC-3654ED321F3B", "D"), // ASP.NET 5
			Guid.ParseExact("603C0E0B-DB56-11DC-BE95-000D561079B0", "D"), // ASP.NET MVC 1
			Guid.ParseExact("F85E285D-A4E0-4152-9332-AB1D724D3325", "D"), // ASP.NET MVC 2
			Guid.ParseExact("E53F8FEA-EAE0-44A6-8774-FFD645390401", "D"), // ASP.NET MVC 3
			Guid.ParseExact("E3E379DF-F4C6-4180-9B81-6769533ABE47", "D"), // ASP.NET MVC 4
			Guid.ParseExact("349C5851-65DF-11DA-9384-00065B846F21", "D"), // ASP.NET MVC 5
		};
	}
}
