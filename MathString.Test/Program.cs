using System;

namespace MathString.Test
{
	class Program
	{
		static void Main(string[] args)
		{
			MathString.AddGlobalVariable(new MathString.Variable("plop", 5));
			MathString m = new MathString();
			string line;
			while ((line = Console.ReadLine()) != "")
			{
				string formule = line;
				try
				{
					string result = m.Convert(formule);
					Console.WriteLine("\"{0}\" = {1}", formule, result);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}
	}
}
