using System;

namespace MathString.Test
{
	class Program
	{
		static void Main(string[] args)
		{
			MathString m = new MathString();
			string line;
			while ((line = Console.ReadLine()) != "")
			{
				string formule = line;
				Console.WriteLine("\"{0}\" = {1}", formule, m.Convert(formule));
			}
		}
	}
}
