using System;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class ServiceFilterTransformation : ITransformation
	{
		private static readonly string[] KnownTestFrameworkIds =
		{
			"Microsoft.NET.Test.Sdk",
			"xUnit.Core",
			"xUnit",
			"NUnit",
		};

		private static readonly Version Vs15TestServiceFixVersion = new Version(15, 7);

		private readonly Version targetVisualStudioVersion;

		public ServiceFilterTransformation(Version targetVisualStudioVersion)
		{
			this.targetVisualStudioVersion = targetVisualStudioVersion ?? throw new ArgumentNullException(nameof(targetVisualStudioVersion));
		}

		public void Transform(Project definition)
		{
			var removeQueue = definition.ItemGroups.ElementsAnyNamespace("Service").Where(IncludeMatchesSpecificGuid).ToArray();

			foreach (var element in removeQueue)
			{
				element.Remove();
			}

			bool IncludeMatchesSpecificGuid(XElement child)
			{
				if (string.Equals(child.Attribute("Include")?.Value, "{82A7F48D-3B50-4B1E-B82E-3ADA8210C358}",
					StringComparison.InvariantCultureIgnoreCase))
				{
					// Fix is included with VS15.7 and above, but we might target VS15.0
					// All known test providers also have a fix included
					return targetVisualStudioVersion >= Vs15TestServiceFixVersion || definition.PackageReferences.Any(IsKnownTestProvider);
				}

				return false;
			}
		}

		private static bool IsKnownTestProvider(PackageReference x)
		{
			// In theory, we should be checking versions of these test frameworks
			// to see, if they have the fix included.

			return KnownTestFrameworkIds.Contains(x.Id);
		}
	}
}
