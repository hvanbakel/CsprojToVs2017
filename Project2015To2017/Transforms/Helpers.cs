using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Transforms
{
	public static class Helpers
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
	}
}