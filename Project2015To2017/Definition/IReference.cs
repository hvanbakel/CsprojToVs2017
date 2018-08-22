using System.Xml.Linq;

namespace Project2015To2017.Definition
{
	public interface IReference
	{
		XElement DefinitionElement { get; }
	}
}