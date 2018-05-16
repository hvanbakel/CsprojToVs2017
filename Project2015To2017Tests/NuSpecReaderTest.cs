using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017.Reading;

namespace Project2015To2017Tests
{
	[TestClass]
    public class NuSpecReaderTest
    {
	    [TestMethod]
	    public void LoadsNuSpecWithNoNamespace()
	    {
		    var reader = new NuSpecReader();
		    var nuspec = reader.Read(new FileInfo(@"TestFiles\nuSpecs\dummy.csproj"), new Progress<string>());

		    Assert.IsNotNull(nuspec);
	    }

    }
}
