using System;
using System.IO;
using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public sealed class ProjectReference : IReference
	{
		public string ProjectName { get; set; }
		public Guid ProjectGuid { get; set; }
		public string ProjectTypeGuid { get; set; }

		public string Include { get; set; }
		public string Aliases { get; set; }
		public bool EmbedInteropTypes { get; set; }

		public FileInfo ProjectFile { get; set; }
		public XElement DefinitionElement { get; set; }

		/// <summary>
		/// Extension of the project file, if any
		/// </summary>
		public string Extension => ProjectFile?.Extension ?? Path.GetExtension(Include);
	}
}