using System;
using Microsoft.Extensions.Logging;

namespace Project2015To2017
{
	public sealed class NoopLogger : ILogger, IDisposable
	{
		public static ILogger Instance = new NoopLogger();

		private NoopLogger()
		{

		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return this;
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return false;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
		}
	}
}