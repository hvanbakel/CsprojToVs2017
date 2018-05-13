using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public class AssemblyInfoReader
	{
		public AssemblyAttributes Read(
			FileInfo projectFile, IProgress<string> progress
		)
		{
			var projectFolder = projectFile.Directory;

			var assemblyInfoFiles = projectFolder
										.EnumerateFiles("AssemblyInfo.cs", SearchOption.AllDirectories)
										.ToArray();

			if (assemblyInfoFiles.Length == 1)
			{
				progress.Report($"Reading assembly info from {assemblyInfoFiles[0].FullName}.");

				var text = File.ReadAllText(assemblyInfoFiles[0].FullName);

				return new AssemblyAttributes
				{
					Description = GetAttributeValue<AssemblyDescriptionAttribute>(text),
					Title = GetAttributeValue<AssemblyTitleAttribute>(text),
					Company = GetAttributeValue<AssemblyCompanyAttribute>(text),
					Product = GetAttributeValue<AssemblyProductAttribute>(text),
					Copyright = GetAttributeValue<AssemblyCopyrightAttribute>(text),
					InformationalVersion = GetAttributeValue<AssemblyInformationalVersionAttribute>(text),
					Version = GetAttributeValue<AssemblyVersionAttribute>(text),
					FileVersion = GetAttributeValue<AssemblyFileVersionAttribute>(text),
					Configuration = GetAttributeValue<AssemblyConfigurationAttribute>(text)
				};
			}
			else
			{
				progress.Report($@"Could not read from assemblyinfo, multiple assemblyinfo files found: 
{string.Join(Environment.NewLine, assemblyInfoFiles.Select(x => x.FullName))}.");
			}

			return null;
		}

		private string GetAttributeValue<T>(string text)
			where T : Attribute
		{
			var attributeTypeName = typeof(T).Name;
			var attributeName = attributeTypeName.Substring(0, attributeTypeName.Length - 9);

			var regex = new Regex($@"\[assembly:.*{attributeName}\(\""(?<value>.*)\""\)]", RegexOptions.Compiled);

			// TODO parse this in roslyn so we actually know that it's not comments.
			var match = regex.Match(text);
			if (match.Groups.Count > 1)
			{
				return match.Groups[1].Value;
			}
			return null;
		}
	}
}