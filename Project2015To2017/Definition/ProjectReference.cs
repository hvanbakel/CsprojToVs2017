using System.IO;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public sealed class ProjectReference : IReference
	{
		public string Include { get; set; }
		public string Aliases { get; set; }
		public bool EmbedInteropTypes { get; set; }

		public FileInfo ProjectFile { get; set; }
		public XElement DefinitionElement { get; set; }
	}
}