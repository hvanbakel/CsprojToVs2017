using System;
using System.IO;

namespace Project2015To2017
{
	public static partial class PathCompat
	{
#if NETCOREAPP2_1
		/// <summary>
		/// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
		/// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
		/// </summary>
		/// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
		/// <param name="path">The destination path.</param>
		/// <returns>The relative path or <paramref name="path"/> if the paths don't share the same root.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
		public static string GetRelativePath(string relativeTo, string path)
		{
			return Path.GetRelativePath(relativeTo, path);
		}
#elif !NETSTANDARD2_0
#error "Revise conditional compilation clauses"
#endif
	}
}
