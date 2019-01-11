using System;
using Project2015To2017.Definition;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W001IllegalProjectTypeDiagnostic : DiagnosticBase
	{
		private static readonly Dictionary<Guid, string> TypeGuids = new Dictionary<Guid, string>
		{
			[Guid.ParseExact("EFBA0AD7-5A72-4C68-AF49-83D382785DCF", "D")] = "Xamarin.Android",
			[Guid.ParseExact("6BC8ED88-2882-458C-8E55-DFD12B67127B", "D")] = "Xamarin.iOS",
			[Guid.ParseExact("A5A43C5B-DE2A-4C0C-9213-0A381AF9435A", "D")] = "UAP/UWP",
		};

		/// <inheritdoc />
		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>(TypeGuids.Count + 1);
			if (project.IsWindowsFormsProject())
			{
				list.Add(CreateDiagnosticResult(project,
					"Windows Forms support in CPS is in early stages and support might depend on your working environment.",
					project.FilePath));
			}

			var guidTypes = project
				.IterateProjectTypeGuids()
				.ToImmutableHashSet();

			foreach (var (guid, source) in guidTypes)
			{
				if (!TypeGuids.TryGetValue(guid, out var description))
					continue;

				list.Add(
					CreateDiagnosticResult(project,
							$"Project type {description} is not tested thoroughly and support might depend on your working environment.",
							project.FilePath)
						.LoadLocationFromElement(source));
			}

			return list;
		}

		public W001IllegalProjectTypeDiagnostic() : base(1)
		{
		}
	}
}