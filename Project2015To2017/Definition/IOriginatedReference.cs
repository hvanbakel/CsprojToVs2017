using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public interface IOriginatedReference
	{
		XElement DefinitionElement { get; }
	}
}