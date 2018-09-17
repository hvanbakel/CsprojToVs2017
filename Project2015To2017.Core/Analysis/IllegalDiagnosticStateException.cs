using System;

namespace Project2015To2017.Analysis
{
	public sealed class IllegalDiagnosticStateException : InvalidOperationException
	{
		/// <inheritdoc />
		public IllegalDiagnosticStateException()
		{
		}

		/// <inheritdoc />
		public IllegalDiagnosticStateException(string message) : base(message)
		{
		}

		/// <inheritdoc />
		public IllegalDiagnosticStateException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}