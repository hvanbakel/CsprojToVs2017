using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.ProjectModel;

namespace Project2015To2017.CleanUp
{
	/// <remarks>
	/// Credit for the stuff happening in here goes to the https://github.com/jaredcnance/dotnet-status project
	/// </remarks>
	public class DependencyGraphService
	{
		public DependencyGraphSpec GenerateDependencyGraph(FileInfo projectPath)
		{
			var dotNetRunner = new DotNetRunner();
			var dgOutput = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
			var arguments = new[] { "msbuild", $"\"{projectPath.FullName}\"", "/t:GenerateRestoreGraphFile", $"/p:RestoreGraphOutputPath=\"{dgOutput}\"" };
			var runStatus = dotNetRunner.Run(projectPath.DirectoryName, arguments);

			if (runStatus.IsSuccess)
			{
				var dependencyGraphText = File.ReadAllText(dgOutput);
				return new DependencyGraphSpec(JsonConvert.DeserializeObject<JObject>(dependencyGraphText));
			}

			throw new Exception($"Unable to process the the project `{projectPath}. Are you sure this is a valid .NET Core or .NET Standard project type?" +
								$"\r\n\r\nHere is the full error message returned from the Microsoft Build Engine:\r\n\r\n" + runStatus.Output);
		}
	}
}