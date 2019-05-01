using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public static partial class Extensions
	{
		public static bool PropertyCondition(this XElement element, out string condition)
		{
			ICollection<string> store = new List<string>();
			do
			{
				var selfCondition = element.Attribute("Condition")?.Value?.Trim();
				if (!string.IsNullOrEmpty(selfCondition))
				{
					store.Add(selfCondition);
				}

				element = element.Parent;
			} while (element != null);

			if (store.Count == 0)
			{
				condition = null;
				return false;
			}

			condition = string.Join(" and ", store.Reverse().Select(x => $"({x})"));
			return true;
		}

		public static IEnumerable<XElement> ElementsAnyNamespace<T>(this IEnumerable<T> source, string localName)
			where T : XContainer
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return source.Elements().Where(e => e.Name.LocalName == localName);
		}

		public static IEnumerable<XElement> ElementsAnyNamespace<T>(this T source, string localName)
			where T : XContainer
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
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
				 select ((n is XElement element) ? RemoveAllNamespaces(element) : n)),
				(e.HasAttributes)
					? (from a in e.Attributes()
					   where (!a.IsNamespaceDeclaration)
					   select new XAttribute(a.Name.LocalName, a.Value))
					: null);
		}

		public static (string value, string attribute, XElement source) ExtractIncludeItemPath(this XElement self)
		{
			var itemPath = self.Attribute("Include")?.Value;
			if (itemPath != null) return (itemPath, "Include", self);
			itemPath = self.Attribute("Update")?.Value;
			if (itemPath != null) return (itemPath, "Update", self);
			itemPath = self.Attribute("Remove")?.Value;
			return itemPath != null ? (itemPath, "Remove", self) : (null, null, self);
		}

		public static IEqualityComparer<string> PathEqualityComparer { get; } = new PathEqualityComparerImpl();

		private class PathEqualityComparerImpl : IEqualityComparer<string>
		{
			public bool Equals(string x, string y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (x is null) return false;
				if (y is null) return false;
				if (x.GetType() != y.GetType()) return false;
				x = x.Replace('\\', '/');
				y = y.Replace('\\', '/');
				return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
			}

			public int GetHashCode(string x)
			{
				return StringComparer.OrdinalIgnoreCase.GetHashCode(x.Replace('\\', '/'));
			}
		}
	}
}