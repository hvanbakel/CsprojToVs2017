using System;
using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Reading;

namespace Project2015To2017.Transforms
{
	public sealed class PropertyDeduplicationTransformation : ITransformation
	{
		public void Transform(Project definition, IProgress<string> progress)
		{
			var props = definition.AdditionalPropertyGroups
				.Where(x => !string.IsNullOrEmpty(x.Attribute("Condition")?.Value))
				.Select(x => (
					x,
					// disabled until better solution to class visibility issue is devised
					// ReSharper disable once PossibleNullReferenceException
					// ConditionEvaluator.GetConditionState(x.Attribute("Condition").Value),
					x.Elements().Select(c => c.Name.LocalName).ToImmutableHashSet()
				))
				.ToImmutableArray();

			var unconditional = definition.AdditionalPropertyGroups
				.First(x => string.IsNullOrEmpty(x.Attribute("Condition")?.Value));

			if (props.Length == 0)
			{
				return;
			}

			var intersection = props.First().Item2;
			foreach (var (group, nameSet) in props)
			{
				intersection = intersection.Intersect(nameSet);
			}

			if (intersection.IsEmpty)
			{
				return;
			}

			foreach (var commonKey in intersection)
			{
				var properties = props.Select(x => x.Item1.Element(x.Item1.Name.Namespace + commonKey)).ToImmutableArray();
				var values = properties.Select(x => x.Value).ToImmutableArray();
				if (values.Distinct().Count() != 1) continue;

				foreach (var property in properties)
				{
					property.Remove();
				}

				unconditional.Add(properties.First());
			}
		}
	}
}