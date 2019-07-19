using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Project2015To2017.Definition;
using Serilog;

namespace Project2015To2017.Migrate2017.Tool
{
	public partial class CommandLogic
	{
		private readonly List<(ImmutableHashSet<(string when, string tfms)> variant, ImmutableArray<string> answer)>
			previousFrameworkResolutionAnswers =
				new List<(ImmutableHashSet<(string, string)>, ImmutableArray<string>)>();


		private bool shownFrameworkInputExamples = false;

		private bool WizardUnknownTargetFrameworkCallback(Project project,
			IReadOnlyList<(IReadOnlyList<string> frameworks, XElement source, string condition)> foundTargetFrameworks)
		{
			Log.Warning("Cannot unambiguously determine target frameworks for {ProjectName}.", project.FilePath);

			var currentVariant = new HashSet<(string when, string tfms)>();

			if (foundTargetFrameworks.Count > 0)
			{
				Log.Information("Found {Count} possible variants:", foundTargetFrameworks.Count);
				foreach (var (frameworks, source, condition) in foundTargetFrameworks)
				{
					if (source is IXmlLineInfo elementOnLine && elementOnLine.HasLineInfo())
					{
						Log.Information("{Line}: {TFMs} ({When})", elementOnLine.LineNumber, frameworks, condition);
					}
					else
					{
						Log.Information("{TFMs} ({When})", frameworks, condition);
					}

					currentVariant.Add((condition,
						string.Join(";", frameworks.ToImmutableSortedSet()).ToLowerInvariant()));
				}

				foreach (var (variant, answer) in previousFrameworkResolutionAnswers)
				{
					if (!currentVariant.SetEquals(variant))
						continue;

					Log.Information("You have previously selected {FormerChoice} for that combination.", answer);
					if (AskBinaryChoice("Would you like to use this framework set?"))
					{
						foreach (var tfm in answer)
						{
							project.TargetFrameworks.Add(tfm);
						}

						return true;
					}

					break;
				}
			}

			Log.Information("Please, enter target frameworks to use (comma or space separated):");

			if (!shownFrameworkInputExamples)
			{
				Log.Information("e.g.: {Example}", string.Join(" ", "net48", "netstandard2.1", "netcoreapp3.0"));
				shownFrameworkInputExamples = true;
			}

			Console.Out.Flush();

			var tfms = Console.ReadLine()
				           ?.Trim()
				           .Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries)
				           .Where(s => !string.IsNullOrWhiteSpace(s))
				           .Select(x => x.Trim())
				           .ToImmutableArray() ?? ImmutableArray<string>.Empty;

			if (tfms.IsDefaultOrEmpty)
			{
				Log.Warning("You didn't specify any TFMs to use.");
				return AskBinaryChoice("Attempt to continue without specifying them?", defaultChoiceIsYes: false);
			}

			if (foundTargetFrameworks.Count > 0)
			{
				previousFrameworkResolutionAnswers.Add((currentVariant.ToImmutableHashSet(), tfms));
			}

			foreach (var tfm in tfms)
			{
				project.TargetFrameworks.Add(tfm);
			}

			return true;
		}
	}
}