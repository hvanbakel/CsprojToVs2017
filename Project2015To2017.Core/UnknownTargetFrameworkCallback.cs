using System.Collections.Generic;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017
{
	/// <summary>
	/// Action that will be executed when project reader cannot unambiguously determine target frameworks
	/// for the currently processing project.
	/// </summary>
	/// <param name="project">Project that is being parsed</param>
	/// <param name="foundTargetFrameworks">A list of ambiguous target frameworks set</param>
	/// <returns>true if the parsing should continue, false if it should be aborted</returns>
	public delegate bool UnknownTargetFrameworkCallback(Project project, IReadOnlyList<(IReadOnlyList<string> frameworks, XElement source, string condition)> foundTargetFrameworks);
}
