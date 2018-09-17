using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Project2015To2017.Analysis
{
	public sealed class LoggerReporter : ReporterBase<LoggerReporterOptions>
	{
		private readonly ILogger logger;

		public LoggerReporter(ILogger logger)
		{
			this.logger = logger;
		}

		public override LoggerReporterOptions DefaultOptions => new LoggerReporterOptions();

		/// <inheritdoc />
		protected override void Report(IDiagnosticResult result, LoggerReporterOptions reporterOptions)
		{
			var consoleHeader = $"{result.Code}: ";
			string linePadding;
			{
				var pad = new StringBuilder(consoleHeader.Length, consoleHeader.Length);
				pad.Append(' ', consoleHeader.Length);
				linePadding = pad.ToString();
			}
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

			this.logger.LogInformation(consoleHeader + Environment.NewLine + message);
		}
	}
}
