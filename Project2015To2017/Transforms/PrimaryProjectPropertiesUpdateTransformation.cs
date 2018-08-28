using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class PrimaryProjectPropertiesUpdateTransformation : ITransformation
	{
		public void Transform(Project project)
		{
			AddTargetFrameworks(project, project.TargetFrameworks);

			var configurations = project.Configurations ?? Array.Empty<string>();
			if (configurations.Count != 0)
				// ignore default "Debug;Release" configuration set
				if (configurations.Count != 2 || !configurations.Contains("Debug") ||
				    !configurations.Contains("Release"))
					project.SetProperty("Configurations", string.Join(";", configurations));

			var platforms = project.Platforms ?? Array.Empty<string>();
			if (platforms.Count != 0)
				// ignore default "AnyCPU" platform set
				if (platforms.Count != 1 || !platforms.Contains("AnyCPU"))
					project.SetProperty("Platforms", string.Join(";", platforms));

			project.SetProperty("AppendTargetFrameworkToOutputPath",
				project.AppendTargetFrameworkToOutputPath ? null : "false");

			project.SetProperty("ExtrasEnableWpfProjectSetup",
				project.IsWindowsPresentationFoundationProject ? "true" : null);
			project.SetProperty("ExtrasEnableWinFormsProjectSetup",
				project.IsWindowsFormsProject ? "true" : null);

			string outputType;
			switch (project.Type)
			{
				case ApplicationType.ConsoleApplication:
					outputType = "Exe";
					break;
				case ApplicationType.WindowsApplication:
					outputType = "WinExe";
					break;
				default:
					outputType = null;
					break;
			}

			project.SetProperty("OutputType", outputType);

			AddPackageNodes(project);

			var propertyGroup = project.PrimaryPropertyGroup();

			if (project.BuildEvents != null && project.BuildEvents.Any())
			{
				foreach (var buildEvent in project.BuildEvents.Select(Extensions.RemoveAllNamespaces))
				{
					propertyGroup.Add(buildEvent);
				}
			}
		}

		private void AddPackageNodes(Project project)
		{
			var packageConfiguration = project.PackageConfiguration;
			if (packageConfiguration == null)
			{
				return;
			}

			//Add those properties not already covered by the project properties

			project.SetProperty("Authors", packageConfiguration.Authors);
			project.SetProperty("PackageIconUrl", packageConfiguration.IconUrl);
			project.SetProperty("PackageId", packageConfiguration.Id);
			project.SetProperty("PackageLicenseUrl", packageConfiguration.LicenseUrl);
			project.SetProperty("PackageProjectUrl", packageConfiguration.ProjectUrl);
			project.SetProperty("PackageReleaseNotes", packageConfiguration.ReleaseNotes);
			project.SetProperty("PackageTags", packageConfiguration.Tags);

			if (packageConfiguration.Id != null && packageConfiguration.Tags == null) project.SetProperty("PackageTags", "Library");

			if (packageConfiguration.RequiresLicenseAcceptance)
			{
				project.SetProperty("PackageRequireLicenseAcceptance", "true");
			}
		}

		private void AddTargetFrameworks(Project project, IList<string> targetFrameworks)
		{
			if (targetFrameworks == null || targetFrameworks.Count == 0)
			{
				return;
			}

			var newElement = targetFrameworks.Count > 1
				? new XElement("TargetFrameworks", string.Join(";", targetFrameworks))
				: new XElement("TargetFramework", targetFrameworks[0]);

			project.ReplacePropertiesWith(newElement,
				"TargetFrameworks", "TargetFramework", "TargetFrameworkVersion");
		}
	}
}