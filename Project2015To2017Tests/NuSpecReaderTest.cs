using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;
using Project2015To2017.Reading;

namespace Project2015To2017Tests
{
	[TestClass]
    public class NuSpecReaderTest
    {
	    [TestMethod]
	    public void LoadsNuSpecWithNoNamespace()
	    {
		    var reader = new NuSpecReader(NoopLogger.Instance);
		    var nuspec = reader.Read(new FileInfo(@"TestFiles\nuSpecs\dummy.csproj"));

		    Assert.IsNotNull(nuspec);
	    }

    }
}
