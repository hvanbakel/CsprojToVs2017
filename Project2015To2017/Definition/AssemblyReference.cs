using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	// Reference
	public sealed class AssemblyReference : IReference
	{
		// Attributes
		public string Include { get; set; }

		// Elements
		public string EmbedInteropTypes { get; set; }
		public string HintPath { get; set; }
		public string Private { get; set; }
		public string SpecificVersion { get; set; }

		public XElement DefinitionElement { get; set; }
	}
}
