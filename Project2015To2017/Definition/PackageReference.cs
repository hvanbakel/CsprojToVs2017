using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public sealed class PackageReference : IOriginatedReference
	{
		public string Id { get; set; }
		public string Version { get; set; }
		public bool IsDevelopmentDependency { get; set; }

		public XElement DefinitionElement { get; set; }
	}
}