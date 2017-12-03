using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Project2015To2017;

namespace Project2015To2017Tests
{
    [TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void ValidatesFileIsWritable()
        {
            File.SetAttributes("TestFiles\\readonly.testcsproj", FileAttributes.ReadOnly);
            Assert.IsFalse(Program.Validate(new FileInfo("TestFiles\\readonly.testcsproj")));
        }

        [TestMethod]
        public void ValidatesFileExists()
        {
            Assert.IsFalse(Program.Validate(new FileInfo("TestFiles\\nonexistent.testcsproj")));
        }
    }
}
