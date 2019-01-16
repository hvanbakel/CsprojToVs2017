using System;
using System.IO;

namespace Project2015To2017
{
	/// <summary>
	/// https://stackoverflow.com/a/31941159
	/// </summary>
	public static partial class Extensions
	{
		/// <summary>
		/// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
		/// The comparison is case-insensitive, handles / and \ slashes as folder separators and
		/// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
		/// </summary>
		public static bool IsSubPathOf(this string path, string baseDirPath)
		{
			var normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
				.WithEnding("\\"));

			var normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
				.WithEnding("\\"));

			return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
		/// results in satisfying .EndsWith(ending).
		/// </summary>
		/// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
		private static string WithEnding(this string str, string ending)
		{
			if (str == null)
				return ending;

			var result = str;

			// Right() is 1-indexed, so include these cases
			// * Append no characters
			// * Append up to N characters, where N is ending length
			for (var i = 0; i <= ending.Length; i++)
			{
				var tmp = result + ending.Right(i);
				if (tmp.EndsWith(ending))
					return tmp;
			}

			return result;
		}

		/// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
		/// <param name="value">The string to retrieve the substring from.</param>
		/// <param name="length">The number of characters to retrieve.</param>
		/// <returns>The substring.</returns>
		private static string Right(this string value, int length)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (length < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(length), length,
					"Length is less than zero");
			}

			return (length < value.Length) ? value.Substring(value.Length - length) : value;
		}
	}
}