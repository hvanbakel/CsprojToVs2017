using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Reading;

namespace Project2015To2017Tests
{
	[TestClass]
	public class SolutionReaderTests
	{
		[TestMethod]
		public void ReadsSolutionFileSuccessfully()
		{
			var testFile = @"TestFiles/Solutions/sampleSolution.testsln";

			var logger = new DummyLogger {MinimumLogLevel = LogLevel.Warning};

			SolutionReader.Instance.Read(testFile, logger);

			//Should be no warnings or errors
			Assert.IsFalse(logger.LogEntries.Any());
		}
	}
}
