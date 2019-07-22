using System;

namespace Project2015To2017.Migrate2017.Tool
{
	public partial class CommandLogic
	{
		private static bool AskBinaryChoice(string question, string yes = "Yes", string no = "No",
			bool defaultChoiceIsYes = true)
		{
			Console.Out.Flush();
			var yesCharLower = char.ToLowerInvariant(yes[0]);
			var noCharLower = char.ToLowerInvariant(no[0]);
			var yesChar = defaultChoiceIsYes ? char.ToUpperInvariant(yes[0]) : yesCharLower;
			var noChar = defaultChoiceIsYes ? noCharLower : char.ToUpperInvariant(no[0]);
			Console.Write($"{question} ({yesChar}/{noChar}) ");
			Console.Out.Flush();
			bool? result = null;
			while (!result.HasValue)
			{
				result = DetermineKeyChoice(Console.ReadKey(true), yesCharLower, noCharLower, defaultChoiceIsYes);
			}

			var realResult = result.Value;
			Console.WriteLine(realResult ? yes : no);
			Console.Out.Flush();
			return realResult;
		}

		private static bool? DetermineKeyChoice(ConsoleKeyInfo info, char yesChar, char noChar, bool defaultChoice)
		{
			switch (char.ToLowerInvariant(info.KeyChar))
			{
				case 'y':
				case 't':
				case '1':
				case char c when c == yesChar:
					return true;
				case 'n':
				case 'f':
				case '0':
				case char c when c == noChar:
					return false;
			}

			switch (info.Key)
			{
				case ConsoleKey.LeftArrow:
					return true;
				case ConsoleKey.RightArrow:
					return false;
				case ConsoleKey.Enter:
					return defaultChoice;
			}

			return null;
		}
	}
}