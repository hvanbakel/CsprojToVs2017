using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Project2015To2017.CleanUp
{
	/// <remarks>
	/// Credit for the stuff happening in here goes to the https://github.com/jaredcnance/dotnet-status project
	/// </remarks>
	public class DotNetRunner
	{
		public RunStatus Run(string workingDirectory, string[] arguments)
		{
			var psi = new ProcessStartInfo("dotnet", string.Join(" ", arguments))
			{
				WorkingDirectory = workingDirectory,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			using (var p = new Process())
			{
				p.StartInfo = psi;
				p.Start();

				var output = new StringBuilder();
				var errors = new StringBuilder();
				var outputTask = ConsumeStreamReaderAsync(p.StandardOutput, output);
				var errorTask = ConsumeStreamReaderAsync(p.StandardError, errors);

				var processExited = p.WaitForExit(20000);

				if (processExited == false)
					return new RunStatus(output.ToString(), errors.ToString(), -1);

				Task.WaitAll(outputTask, errorTask);

				return new RunStatus(output.ToString(), errors.ToString(), p.ExitCode);
			}
		}

		private static async Task ConsumeStreamReaderAsync(TextReader reader, StringBuilder lines)
		{
			await Task.Yield();

			string line;
			while ((line = await reader.ReadLineAsync()) != null)
			{
				lines.AppendLine(line);
			}
		}
	}
}