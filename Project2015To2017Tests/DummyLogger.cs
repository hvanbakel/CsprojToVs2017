using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Project2015To2017Tests
{
	internal class DummyLogger : ILogger
	{
		private readonly List<string> logs = new List<string>();
		public IReadOnlyList<string> LogEntries => this.logs;

		public LogLevel MinimumLogLevel { get; set; } = LogLevel.Error;

		public IDisposable BeginScope<TState>(TState state)
		{
			throw new NotImplementedException();
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel >= MinimumLogLevel;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (IsEnabled(logLevel))
			{
				this.logs.Add(formatter(state, exception));
			}
		}
	}
}
