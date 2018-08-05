using System;
using System.IO;
using System.Text;

namespace Project2015To2017.Analysis
{
	public class ConsoleReporter : ReporterBase
	{
		public static readonly ConsoleReporter Instance = new ConsoleReporter();

		/// <inheritdoc />
		protected override void Report(string code, string message, string source = null, uint sourceLine = uint.MaxValue)
		{
			var consoleHeader = $"{code}: ";
			string linePadding;
			{
				var pad = new StringBuilder(consoleHeader.Length, consoleHeader.Length);
				pad.Append(' ', consoleHeader.Length);
				linePadding = pad.ToString();
			}
			Console.Write(consoleHeader);
			message = message.Trim();

			switch (source)
			{
				case string _ when sourceLine != uint.MaxValue:
					message = $"{source}:{sourceLine}: {message}";
					break;
				case string _ when sourceLine == uint.MaxValue:
					message = $"{source}: {message}";
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
