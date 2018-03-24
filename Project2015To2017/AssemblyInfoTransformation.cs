using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using hvanbakel.Project2015To2017.Definition;

namespace hvanbakel.Project2015To2017
{
    internal sealed class AssemblyInfoTransformation : ITransformation
    {
        public async Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            var assemblyInfoFiles = projectFolder
                .EnumerateFiles("AssemblyInfo.cs", SearchOption.AllDirectories)
                .ToArray();

            if (assemblyInfoFiles.Length == 1)
            {
                Console.WriteLine($"Reading assembly info from {assemblyInfoFiles[0].FullName}.");

                string text;
                using (var filestream = File.Open(assemblyInfoFiles[0].FullName, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(filestream))
                {
                    text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                }

                definition.AssemblyAttributes = new AssemblyAttributes
                {
                    AssemblyName = definition.AssemblyName ?? projectFolder.Name,
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
                Console.WriteLine($@"Could not read from assemblyinfo, multiple assemblyinfo files found: 
{string.Join(Environment.NewLine, assemblyInfoFiles.Select(x => x.FullName))}.");
            }
        }

        private string GetAttributeValue<T>(string text)
            where T : Attribute
        {
            var attributeTypeName = typeof(T).Name;
            var attributeName = attributeTypeName.Substring(0, attributeTypeName.Length - 9);

            //var regex = new Regex($@"\[assembly:.*{attributeName}\(\""(?<value>.*)\""\)]", RegexOptions.Compiled);
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