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
            var targetFramework = propertyGroups.Elements(nsSys + "TargetFrameworkVersion").FirstOrDefault()?.Value;

            definition.Optimize = "true".Equals(propertyGroups.Elements(nsSys + "Optimize").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
            definition.TreatWarningsAsErrors = "true".Equals(propertyGroups.Elements(nsSys + "TreatWarningsAsErrors").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
            definition.AllowUnsafeBlocks = "true".Equals(propertyGroups.Elements(nsSys + "AllowUnsafeBlocks").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
            definition.DefineConstants = propertyGroups.Elements(nsSys + "DefineConstants").FirstOrDefault()?.Value;

            definition.RootNamespace = propertyGroups.Elements(nsSys + "RootNamespace").FirstOrDefault()?.Value;
            definition.AssemblyName = propertyGroups.Elements(nsSys + "AssemblyName").FirstOrDefault()?.Value;
            definition.Type = propertyGroups.Elements(nsSys + "TestProjectType").Any() 
                ? ApplicationType.TestProject
                : ToApplicationType(propertyGroups.Elements(nsSys + "OutputType").FirstOrDefault()?.Value);
            definition.TargetFrameworks = new[] { ToTargetFramework(targetFramework) };
            definition.ConditionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") != null).ToArray();

            if (definition.Type == ApplicationType.Unkown)
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
            switch(outputType)
            {
                case "Exe": return ApplicationType.ConsoleApplication;
                case "Library": return ApplicationType.ClassLibrary;
                case "Winexe": return ApplicationType.WindowsApplication;
                default: throw new NotSupportedException($"OutputType {outputType} is not supported.");
            }
        }
    }
}