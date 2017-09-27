using Project2015To2017.Definition;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017
{
    internal sealed class ProjectPropertiesTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
            var propertyGroups = projectFile.Element(nsSys + "Project").Elements(nsSys + "PropertyGroup");

			var unconditionalPropertyGroup = propertyGroups.FirstOrDefault(x => x.Attribute("Condition") == null);
			if (unconditionalPropertyGroup == null)
			{
				throw new NotSupportedException("No unconditional property group found. Cannot determine important properties like target framework and others.");
			}
			else
			{
				var targetFramework = unconditionalPropertyGroup.Elements(nsSys + "TargetFrameworkVersion").FirstOrDefault()?.Value;

				definition.Optimize = "true".Equals(unconditionalPropertyGroup.Elements(nsSys + "Optimize").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
				definition.TreatWarningsAsErrors = "true".Equals(unconditionalPropertyGroup.Elements(nsSys + "TreatWarningsAsErrors").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
				definition.AllowUnsafeBlocks = "true".Equals(unconditionalPropertyGroup.Elements(nsSys + "AllowUnsafeBlocks").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);

				definition.RootNamespace = unconditionalPropertyGroup.Elements(nsSys + "RootNamespace").FirstOrDefault()?.Value;
				definition.AssemblyName = unconditionalPropertyGroup.Elements(nsSys + "AssemblyName").FirstOrDefault()?.Value;
				definition.Type = unconditionalPropertyGroup.Elements(nsSys + "TestProjectType").Any()
					? ApplicationType.TestProject
					: ToApplicationType(unconditionalPropertyGroup.Elements(nsSys + "OutputType").FirstOrDefault()?.Value);
				definition.TargetFrameworks = new[] { ToTargetFramework(targetFramework) };
			}

            definition.ConditionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") != null).ToArray();

            if (definition.Type == ApplicationType.Unknown)
            {
                throw new NotSupportedException("Unable to parse output type.");
            }


            return Task.CompletedTask;
        }

        private string ToTargetFramework(string targetFramework)
        {
            if (targetFramework.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return "net" + targetFramework.Substring(1).Replace(".", string.Empty);
            }

            throw new NotSupportedException($"Target framework {targetFramework} is not supported.");
        }

        private ApplicationType ToApplicationType(string outputType)
        {
            if (string.IsNullOrWhiteSpace(outputType)) 
	    {
	    	return ApplicationType.Unknown;
	    }
	    
            switch(outputType.ToLowerInvariant())
            {
                case "exe": return ApplicationType.ConsoleApplication;
                case "library": return ApplicationType.ClassLibrary;
                case "winexe": return ApplicationType.WindowsApplication;
                default: throw new NotSupportedException($"OutputType {outputType} is not supported.");
            }
        }
    }
}
