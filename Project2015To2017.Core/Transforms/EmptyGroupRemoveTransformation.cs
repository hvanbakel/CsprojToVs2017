using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public class EmptyGroupRemoveTransformation
		: ITransformationWithTargetMoment, ITransformationWithDependencies
	{
		public void Transform(Project definition)
		{
			definition.PropertyGroups = FilterNonEmpty(definition.PropertyGroups);
			definition.ItemGroups = FilterNonEmpty(definition.ItemGroups).ToList();
		}

		private static IReadOnlyList<XElement> FilterNonEmpty(IEnumerable<XElement> groups)
		{
			var (keep, remove) = groups
				.Split(x => x.HasElements
				            || (x.HasAttributes && x.Attributes().Any(a => a.Name.LocalName != "Condition")));
			foreach (var element in remove)
			{
				element.Remove();
			}

			return keep;
		}

		public TargetTransformationExecutionMoment ExecutionMoment =>
			TargetTransformationExecutionMoment.Late;

		public IReadOnlyCollection<string> DependOn => new[]
		{
			typeof(PrimaryProjectPropertiesUpdateTransformation).Name,
		};
	}
}