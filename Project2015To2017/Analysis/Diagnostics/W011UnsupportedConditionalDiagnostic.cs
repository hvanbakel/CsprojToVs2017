using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Reading.Conditionals;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W011UnsupportedConditionalDiagnostic : DiagnosticBase
	{
		public W011UnsupportedConditionalDiagnostic() : base(11)
		{
		}

		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>();
			foreach (var x in project.ProjectDocument.Descendants())
			{
				var condition = x.Attribute("Condition");
				if (condition == null)
				{
					continue;
				}

				var conditionState = ConditionEvaluator.GetConditionState(condition.Value);

				var pairs = conditionState.UnsupportedNodes
					.Select(n => n.GetType().Name.Replace("ExpressionNode", ""))
					.GroupBy(n => n)
					.Select(n => (n.Key, n.Count()));

				foreach (var (key, value) in pairs)
				{
					var countContext = value > 1 ? $"({value} occurrences)" : "";
					list.Add(CreateDiagnosticResult(project,
							$"Unsupported '{key}' expression in conditional {countContext}",
							project.FilePath)
						.LoadLocationFromElement(x));
				}
			}

			return list;
		}
	}
}