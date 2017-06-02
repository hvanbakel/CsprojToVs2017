using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;
using System.Linq;

namespace Project2015To2017
{
    internal sealed class AssemblyReferenceTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";

            definition.AssemblyReferences = projectFile
				.Element(nsSys + "Project")
				.Elements(nsSys + "ItemGroup")
				.Elements(nsSys + "Reference")
				.Where(x => !x.Elements(nsSys + "HintPath").Any())
				.Select(x => x.Attribute("Include").Value).ToArray();

            return Task.CompletedTask;
        }
    }
}
