using System;

namespace Project2015To2017.Console
{
    class Program
    {
        static void Main(string[] args)
        {
	        if (args.Length == 0)
	        {
		        System.Console.WriteLine($"Please specify a project file.");
		        return;
	        }

			var writer = new Project2015To2017.Writing.ProjectWriter();
			foreach (var definition in ProjectConverter.Convert(args[0], new Progress<string>(System.Console.WriteLine)))
			{
				writer.Write(definition);
			}
        }
    }
}
