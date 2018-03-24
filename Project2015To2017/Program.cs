using System;

namespace hvanbakel.Project2015To2017.App
{
    class Program
    {
        static void Main(string[] args)
        {
	        if (args.Length == 0)
	        {
		        Console.WriteLine($"Please specify a project file.");
		        return;
	        }

			ProjectConverter.Convert(args[0], new Progress<string>(Console.WriteLine));
        }
    }
}
