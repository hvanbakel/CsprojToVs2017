using System.IO;

namespace Project2015To2017.Analysis
{
	public interface IReporter
	{
		/// <summary>
		/// Do the actual issue reporting
		/// </summary>
		/// <param name="code">Diagnostic code</param>
		/// <param name="message">Informative message about the issue</param>
		/// <param name="source">File or directory path for user reference</param>
		/// <param name="sourceLine">File line for user reference</param>
		void Report(string code, string message, string source = null, uint sourceLine = uint.MaxValue);

		/// <summary>
		/// Do the actual issue reporting
		/// </summary>
		/// <param name="code">Diagnostic code</param>
		/// <param name="message">Informative message about the issue</param>
		/// <param name="source">File or directory for user reference</param>
		/// <param name="sourceLine">File line for user reference</param>
		void Report(string code, string message, FileSystemInfo source = null, uint sourceLine = uint.MaxValue);
	}
}