using System.IO;

namespace Project2015To2017.Definition
{
	public class ProjectReference
	{
		public string Include { get; set; }
		public string Aliases { get; set; }
		public bool EmbedInteropTypes { get; set; }

		public FileInfo ProjectFile { get; set; }
	}
}
