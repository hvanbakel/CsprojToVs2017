using DotNet.Globbing;
using Serilog;
using System.Collections.Generic;
using System.IO;

namespace Project2015To2017.Migrate2017.Tool
{
	public class CommandLogic
	{
		private readonly PatternProcessor globProcessor = (converter, pattern, callback, self) =>
		{
			Log.Verbose("Falling back to globbing");
			self.DoProcessableFileSearch();
			var glob = Glob.Parse(pattern);
			Log.Verbose("Parsed glob {Glob}", glob);
			foreach (var (path, extension) in self.Files)
			{
				if (!glob.IsMatch(path)) continue;
				var file = new FileInfo(path);
				callback(file, extension);
			}

			return true;
		};

		private readonly Facility facility;

		public CommandLogic()
		{
			var genericLogger = new Serilog.Extensions.Logging.SerilogLoggerProvider().CreateLogger(nameof(Serilog));
			facility = new Facility(genericLogger, globProcessor);
		}

		public void ExecuteEvaluate(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			facility.ExecuteEvaluate(items, conversionOptions);
		}

		public void ExecuteMigrate(
			IReadOnlyCollection<string> items,
			bool noBackup,
			ConversionOptions conversionOptions)
		{
			facility.ExecuteMigrate(items, noBackup, conversionOptions);
		}

		public void ExecuteAnalyze(
			IReadOnlyCollection<string> items,
			ConversionOptions conversionOptions)
		{
			facility.ExecuteAnalyze(items, conversionOptions);
		}
	}
}