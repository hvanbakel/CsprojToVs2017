using Project2015To2017.Definition;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017
{
    internal interface ITransformation
    {
        Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition);
    }
}
