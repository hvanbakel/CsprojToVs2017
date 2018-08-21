using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis
{
	public static class ExtensionMethods
	{
		public static string GetRelativePathTo(this FileSystemInfo from, FileSystemInfo to)
		{
			string GetPath(FileSystemInfo fsi)
			{
				return (fsi is DirectoryInfo d) ? (d.FullName.TrimEnd('\\') + "\\") : fsi.FullName;
			}

			var fromPath = GetPath(from);
			var toPath = GetPath(to);

			var fromUri = new Uri(fromPath);
			var toUri = new Uri(toPath);

			var relativeUri = fromUri.MakeRelativeUri(toUri);
			var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			return relativePath.Replace('/', Path.DirectorySeparatorChar);
		}

		public static DirectoryInfo TryFindBestRootDirectory(this Project project)
		{
			if (project == null) throw new ArgumentNullException(nameof(project));

			return project.Solution?.FilePath.Directory ?? project.FilePath.Directory;
		}

		public static string GetSourcePath(this IDiagnosticLocation self)
		{
			if (self == null) throw new ArgumentNullException(nameof(self));

			return self.SourcePath ?? self.Source?.FullName;
		}

		public static IDiagnosticResult LoadLocationFromElement(this IDiagnosticResult self, XElement element)
		{
			if (self == null) throw new ArgumentNullException(nameof(self));
			if (element == null) throw new ArgumentNullException(nameof(element));

			if (self.Location.SourceLine != uint.MaxValue)
			{
				return self;
			}

			if (element is IXmlLineInfo elementOnLine && elementOnLine.HasLineInfo())
			{
				return new DiagnosticResult(self)
				{
					Location = new DiagnosticLocation(self.Location)
					{
						SourceLine = (uint) elementOnLine.LineNumber
					}
				};
			}

			return self;
		}

		public static StringComparison BestAvailableStringIgnoreCaseComparison
		{
			get
			{
				StringComparison comparison;
#if NETSTANDARD2_0
				comparison = StringComparison.InvariantCultureIgnoreCase;
#else
				comparison = StringComparison.OrdinalIgnoreCase;
#endif
				return comparison;
			}
		}
	}
}