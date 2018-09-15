using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace Project2015To2017
{
	public static class Extensions
	{
		public static string GetRelativePathTo(this FileSystemInfo from, FileSystemInfo to)
		{
			string GetPath(FileSystemInfo fsi)
			{
				return (fsi is DirectoryInfo d) ? (d.FullName.TrimEnd('\\') + "\\") : fsi.FullName;
			}

			var fromPath = GetPath(@from);
			var toPath = GetPath(to);

			var fromUri = new Uri(fromPath);
			var toUri = new Uri(toPath);

			var relativeUri = fromUri.MakeRelativeUri(toUri);
			var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			return relativePath.Replace('/', Path.DirectorySeparatorChar);
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
					select ((n is XElement element) ? RemoveAllNamespaces(element) : n)),
				(e.HasAttributes)
					? (from a in e.Attributes()
						where (!a.IsNamespaceDeclaration)
						select new XAttribute(a.Name.LocalName, a.Value))
					: null);
		}

		/// <summary>
		/// If on Unix, convert backslashes to slashes for strings that resemble paths.
		/// The heuristic is if something resembles paths (contains slashes) check if the
		/// first segment exists and is a directory.
		/// Use a native shared method to massage file path. If the file is adjusted,
		/// that qualifies is as a path.
		///
		/// @baseDirectory is just passed to LooksLikeUnixFilePath, to help with the check
		/// </summary>
		public static string MaybeAdjustFilePath(string value, string baseDirectory = "")
		{
			const StringComparison comparisonType = StringComparison.Ordinal;

			// Don't bother with arrays or properties or network paths, or those that
			// have no slashes.
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			    || string.IsNullOrEmpty(value)
			    || value.StartsWith("$(", comparisonType) || value.StartsWith("@(", comparisonType)
			    || value.StartsWith("\\\\", comparisonType))
			{
				return value;
			}

			// For Unix-like systems, we may want to convert backslashes to slashes
			var newValue = ConvertToUnixSlashes(value);

			// Find the part of the name we want to check, that is remove quotes, if present
			var shouldAdjust = newValue.IndexOf('/') != -1 &&
			                   LooksLikeUnixFilePath(RemoveQuotes(newValue), baseDirectory);
			return shouldAdjust ? newValue : value;
		}

		private static string ConvertToUnixSlashes(string path)
		{
			if (path.IndexOf('\\') == -1)
			{
				return path;
			}

			var unixPath = new StringBuilder(path.Length);
			CopyAndCollapseSlashes(path, unixPath);
			return unixPath.ToString();
		}

		private static void CopyAndCollapseSlashes(string str, StringBuilder copy)
		{
			// Performs Regex.Replace(str, @"[\\/]+", "/")
			for (var i = 0; i < str.Length; i++)
			{
				var isCurSlash = IsAnySlash(str[i]);
				var isPrevSlash = i > 0 && IsAnySlash(str[i - 1]);

				if (!isCurSlash || !isPrevSlash)
				{
					copy.Append(str[i] == '\\' ? '/' : str[i]);
				}
			}
		}

		private static string RemoveQuotes(string path)
		{
			var endId = path.Length - 1;
			const char singleQuote = '\'';
			const char doubleQuote = '\"';

			var hasQuotes = path.Length > 2
			                 && (path[0] == singleQuote && path[endId] == singleQuote
			                     || path[0] == doubleQuote && path[endId] == doubleQuote);

			return hasQuotes ? path.Substring(1, endId - 1) : path;
		}

		private static bool IsAnySlash(char c) => c == '/' || c == '\\';

		/// <summary>
		/// If on Unix, check if the string looks like a file path.
		/// The heuristic is if something resembles paths (contains slashes) check if the
		/// first segment exists and is a directory.
		///
		/// If @baseDirectory is not null, then look for the first segment exists under
		/// that
		/// </summary>
		private static bool LooksLikeUnixFilePath(string value, string baseDirectory = "")
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return false;
			}

			// The first slash will either be at the beginning of the string or after the first directory name
			var directoryLength = value.IndexOf('/', 1) + 1;
			var shouldCheckDirectory = directoryLength != 0;

			// Check for actual files or directories under / that get missed by the above logic
			var shouldCheckFileOrDirectory = !shouldCheckDirectory && value.Length > 0 && value[0] == '/';

			return shouldCheckDirectory && Directory.Exists(Path.Combine(baseDirectory, value.Substring(0, directoryLength)))
			       || shouldCheckFileOrDirectory && (File.Exists(value) || Directory.Exists(value));
		}
	}
}