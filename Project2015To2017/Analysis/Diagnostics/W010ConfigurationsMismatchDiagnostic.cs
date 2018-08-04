using Project2015To2017.Definition;
using Project2015To2017.Reading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Analysis.Diagnostics
{
	public class W010ConfigurationsMismatchDiagnostic : DiagnosticBase
	{
		/// <inheritdoc />
		public W010ConfigurationsMismatchDiagnostic() : base(10)
		{
		}

		/// <param name="project"></param>
		/// <inheritdoc />
		protected override void AnalyzeImpl(Project project)
		{
			var propertyGroups = project.ProjectDocument.Element(project.XmlNamespace + "Project").Elements(project.XmlNamespace + "PropertyGroup").ToArray();

			var configurationSet = new HashSet<string>();
			var platformSet = new HashSet<string>();

			var configurationsFromProperty = ParseFromProperty("Configurations") ?? new[] { "Debug", "Release" };
			var platformsFromProperty = ParseFromProperty("Platforms") ?? new[] { "AnyCPU" };

			foreach (var configuration in configurationsFromProperty)
			{
				configurationSet.Add(configuration);
			}

			foreach (var platform in platformsFromProperty)
			{
				platformSet.Add(platform);
			}

			foreach (var x in project.ProjectDocument.Descendants())
			{
				var condition = x.Attribute("Condition");
				if (condition == null)
				{
					continue;
				}

				var conditionValue = condition.Value;
				if (!conditionValue.Contains("$(Configuration)") && !conditionValue.Contains("$(Platform)"))
				{
					continue;
				}

				var conditionEvaluated = ConditionEvaluator.GetConditionValues(conditionValue);

				if (conditionEvaluated.TryGetValue("Configuration", out var configurations))
				{
					foreach (var configuration in configurations)
					{
						if (!configurationSet.Contains(configuration))
						{
							Report($"Configuration '{configuration}' is used in project file but not mentioned in $(Configurations).", x, project.FilePath);
						}
					}
				}

				if (conditionEvaluated.TryGetValue("Platform", out var platforms))
				{
					foreach (var platform in platforms)
					{
						if (!platformSet.Contains(platform))
						{
							Report($"Platform '{platform}' is used in project file but not mentioned in $(Platforms).", x, project.FilePath);
						}
					}
				}
			}

			string[] ParseFromProperty(string name) => propertyGroups.Where(x => x.Attribute("Condition") == null).Elements(project.XmlNamespace + name)
				.FirstOrDefault()
				?.Value
				.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
