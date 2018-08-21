using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Transforms
{
	public static class ExtensionMethods
	{
		public static IEnumerable<XElement> ElementsAnyNamespace<T>(this IEnumerable<T> source, string localName)
			where T : XContainer
		{
			return source.Elements().Where(e => e.Name.LocalName == localName);
		}

		public static IEnumerable<XElement> ElementsAnyNamespace<T>(this T source, string localName)
			where T : XContainer
		{
			return source.Elements().Where(e => e.Name.LocalName == localName);
		}

		public static (IReadOnlyList<T> @true, IReadOnlyList<T> @false) Split<T>(this IEnumerable<T> source,
			Func<T, bool> predicate)
		{
			List<T> @true = new List<T>(), @false = new List<T>();
			foreach (var item in source)
			{
				if (predicate(item))
				{
					@true.Add(item);
				}
				else
				{
					@false.Add(item);
				}
			}

			return (@true, @false);
		}

		public static bool ValidateChildren(IEnumerable<XElement> value, params string[] expected)
		{
			var defines = value.Select(x => x.Name.LocalName);
			return ValidateSet(defines, expected);
		}

		public static bool ValidateSet(IEnumerable<string> items, IEnumerable<string> expected)
		{
			var set = new HashSet<string>(items);
			foreach (var expecto in expected)
			{
				if (!set.Remove(expecto))
				{
					return false;
				}
			}

			return set.Count == 0;
		}

		public static XElement RemoveAllNamespaces(XElement e)
		{
			return new XElement(e.Name.LocalName,
				(from n in e.Nodes()
					select ((n is XElement) ? RemoveAllNamespaces((XElement) n) : n)),
				(e.HasAttributes)
					? (from a in e.Attributes()
						where (!a.IsNamespaceDeclaration)
						select new XAttribute(a.Name.LocalName, a.Value))
					: null);
		}
	}
}