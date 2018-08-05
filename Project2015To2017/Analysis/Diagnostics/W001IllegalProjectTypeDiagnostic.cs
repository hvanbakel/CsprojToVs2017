using Project2015To2017.Definition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W001IllegalProjectTypeDiagnostic : DiagnosticBase
	{
		private static readonly uint DiagnoticId = 1;
		public static readonly string Code = DiagnoticId.ToDiagnosticCode();
		public override uint Id => DiagnoticId;

		private static readonly Dictionary<string, string> TypeGuids = new Dictionary<string, string>
		{
			["{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"] = "Xamarin.Android",
			["{6BC8ED88-2882-458C-8E55-DFD12B67127B}"] = "Xamarin.iOS",
			["{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}"] = "UAP/UWP",
		};

		/// <param name="project"></param>
		/// <inheritdoc />
		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>(TypeGuids.Count + 1);
			if (project.IsWindowsFormsProject)
			{
				list.Add(CreateDiagnosticResult($"Windows Forms support in CPS is in early stages and support might depend on your working environment.", null, project.FilePath));
			}

			// try to get project type - may not exist
			var typeElement = project.ProjectDocument.Descendants(project.XmlNamespace + "ProjectTypeGuids").FirstOrDefault();
			if (typeElement == null)
			{
				return System.Array.Empty<IDiagnosticResult>();
			}

			// parse the CSV list
			var guidTypes = typeElement.Value
				.Split(';')
				.Select(x => x.Trim().ToUpperInvariant())
				.ToImmutableHashSet();

			foreach (var item in TypeGuids)
			{
				if (!guidTypes.Contains(item.Key)) continue;

				list.Add(CreateDiagnosticResult($"Project type {item.Value} is not tested thoroughly and support might depend on your working environment.", typeElement, project.FilePath));
			}

			return list;
		}
	}
}
