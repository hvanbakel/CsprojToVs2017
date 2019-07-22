using System;
using System.Collections.Generic;
using System.IO;
using Project2015To2017.Definition;
using Project2015To2017.Writing;
using Serilog;

namespace Project2015To2017.Migrate2017.Tool
{
	public partial class CommandLogic
	{
		private void WizardMigrate(IReadOnlyList<Project> legacy, ITransformationSet transformationSet,
			ConversionOptions conversionOptions)
		{
			var transformations = transformationSet.CollectAndOrderTransformations(facility.Logger, conversionOptions);

			var doBackups = AskBinaryChoice("Would you like to create backups?");

			var writer = new ProjectWriter(facility.Logger, new ProjectWriteOptions {MakeBackups = doBackups});

			foreach (var project in legacy)
			{
				using (facility.Logger.BeginScope(project.FilePath))
				{
					var projectName = Path.GetFileNameWithoutExtension(project.FilePath.Name);
					Log.Information("Converting {ProjectName}...", projectName);

					if (!project.Valid)
					{
						Log.Error("Project {ProjectName} is marked as invalid, skipping...", projectName);
						continue;
					}

					foreach (var transformation in transformations.WhereSuitable(project, conversionOptions))
					{
						try
						{
							transformation.Transform(project);
						}
						catch (Exception e)
						{
							Log.Error(e, "Transformation {Item} has thrown an exception, skipping...",
								transformation.GetType().Name);
						}
					}

					if (!writer.TryWrite(project))
						continue;
					Log.Information("Project {ProjectName} has been converted", projectName);
				}
			}
		}
	}
}