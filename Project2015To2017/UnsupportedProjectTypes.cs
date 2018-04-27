using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Project2015To2017
{
	/// <summary>
	/// Helper library to filter out unsupported project types
	/// </summary>
	public static class UnsupportedProjectTypes
	{
		/// <summary>
		/// Check for unsupported ProjectTypeGuids in project
		/// </summary>
		/// <param name="xmlDocument">source project document to check</param>
		/// <returns></returns>
		public static bool IsUnsupportedProjectType(XDocument xmlDocument)
		{
			if (xmlDocument == null) throw new ArgumentNullException(nameof(xmlDocument));
			// try to get project type - may not exist 
			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
			var typeElement = xmlDocument.Descendants(nsSys + "ProjectTypeGuids").FirstOrDefault();
			// no matching tag found, project should be okay to convert
			if (typeElement == null) return false;

			// parse the CSV list
			var guidTypes = typeElement.Value.Split(';').Select(x => x.Trim());

			// if any guid matches an unsupported type, return true
			return (from guid in guidTypes
					from unsupported in unsupportedGuids
					where guid.Equals(unsupported, StringComparison.CurrentCultureIgnoreCase)
					select unsupported).Any();
		}

		/// <summary>
		/// Guids that cannot be converted
		/// </summary>
		/// <remarks>
		/// Taken from https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
		/// Only ASP.NET Apps added at present
		/// Note that the list here is in upper case but project file guids are normally lower case
		/// </remarks>
		private static readonly string[] unsupportedGuids =
			{
			"{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}",  // ASP.NET 5			
			"{603C0E0B-DB56-11DC-BE95-000D561079B0}",  // ASP.NET MVC 
			"{F85E285D-A4E0-4152-9332-AB1D724D3325}",  // ASP.NET MVC 
			"{E53F8FEA-EAE0-44A6-8774-FFD645390401}",  // ASP.NET MVC 
			"{E3E379DF-F4C6-4180-9B81-6769533ABE47}",  // ASP.NET MVC 
			"{349C5851-65DF-11DA-9384-00065B846F21}"   // ASP.NET MVC 
			};
	}
}
