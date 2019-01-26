using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class UpgradeUseComVisibleDefaultTransformation : ITransformation
	{
		public void Transform(Project definition)
		{
			var removeQueue = definition.PropertyGroups
				.ElementsAnyNamespace("ComVisible")
				.Where(x => !string.IsNullOrEmpty(x.Value))
				.ToImmutableArray();

			foreach (var element in removeQueue)
			{
				element.Remove();
			}
		}
	}
}