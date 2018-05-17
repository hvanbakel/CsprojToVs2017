using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;

namespace Project2015To2017Tests
{
	[TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void ValidatesFileExists()
        {
			
	        var progress = new Progress<string>(x => { });

            Assert.IsFalse(ProjectConverter.Validate(new FileInfo("TestFiles\\OtherTestProjects\\nonexistent.testcsproj"), progress));
        }
    }
}
