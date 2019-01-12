using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class UpgradeDebugTypeTransformation : ITransformation
	{
		public void Transform(Project definition)
		{
			var removeQueue = definition.PropertyGroups
				.ElementsAnyNamespace("DebugType")
				.Where(x => !string.Equals(x.Value, "portable", Extensions.BestAvailableStringIgnoreCaseComparison))
				.ToImmutableArray();

			foreach (var element in removeQueue)
			{
				element.Remove();
			}
		}
	}
}
