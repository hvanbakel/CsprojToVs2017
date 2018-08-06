using System;
using System.Text;

namespace Project2015To2017.Analysis
{
	public sealed class ConsoleReporter : ReporterBase<ConsoleReporterOptions>
	{
		public static readonly ConsoleReporter Instance = new ConsoleReporter();

		public override ConsoleReporterOptions DefaultOptions => new ConsoleReporterOptions();

		/// <inheritdoc />
		protected override void Report(IDiagnosticResult result, ConsoleReporterOptions reporterOptions)
		{
			var consoleHeader = $"{result.Code}: ";
			string linePadding;
			{
				var pad = new StringBuilder(consoleHeader.Length, consoleHeader.Length);
				pad.Append(' ', consoleHeader.Length);
				linePadding = pad.ToString();
			}
			Console.Write(consoleHeader);
			var message = result.Message.Trim();

			var sourcePath = result.Location.GetSourcePath();
			var sourceLine = result.Location.SourceLine;
			switch (sourcePath)
			{
				case string _ when sourceLine != uint.MaxValue:
					message = $"{sourcePath}:{sourceLine}: {message}";
					break;
				case string _ when sourceLine == uint.MaxValue:
					message = $"{sourcePath}: {message}";
					break;
				case null when sourceLine != uint.MaxValue:
					message = $"{sourceLine}: {message}";
					break;
				case null when sourceLine == uint.MaxValue:
				default:
					break;
			}

			uint messageLineIndex = 0;
			foreach (var messageLine in message.Split('\n'))
			{
				if (messageLineIndex != 0)
				{
					Console.Write(linePadding);
				}

				Console.WriteLine(messageLine);
				++messageLineIndex;
			}
		}
	}
}
