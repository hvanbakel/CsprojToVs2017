using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class PropertyDeduplicationTransformation : ITransformationWithDependencies
	{
		public void Transform(Project definition)
		{
			var props = definition
				.ConditionalGroups()
				.Select(x => (
					x,
					x.Elements()
						.Where(c => !c.HasElements)
						.Select(c => c.Name.LocalName)
						.ToImmutableHashSet()
				))
				.ToImmutableArray();

			if (props.Length == 0)
			{
				return;
			}

			var intersection = props.First().Item2;
			foreach (var (_, nameSet) in props.Skip(1))
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
				var values = properties.Select(x => x.Value).ToImmutableHashSet();
				if (values.Count != 1) continue;

				foreach (var property in properties)
				{
					property.Remove();
				}

				var sourceForCopy = properties.First();
				definition.PrimaryPropertyGroup().Add(sourceForCopy);
			}
		}

		public IReadOnlyCollection<string> DependOn => new[]
		{
			typeof(PropertySimplificationTransformation).Name,
		};
	}
}