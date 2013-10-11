using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace MathString
{
	public class MathString
	{
		#region Inner-Class
		public class MathStringTemplate
		{

			#region Attributs
			private readonly int _weigth;
			private readonly MathAction _action;
			private readonly string _operator;

			#endregion

			#region Constructeur
			public MathStringTemplate(string @operator, int weigth, MathAction action)
			{
				_operator = @operator;
				_weigth = weigth;
				_action = action;
			}
			#endregion

			#region Propriete
			public int Weigth
			{
				get { return _weigth; }
			}
			public MathAction Action
			{
				get { return _action; }
			}
			public string Operator
			{
				get { return _operator; }
			}
			#endregion

		}
		#endregion

		#region Attributs
		public const string NumberRegex = @"-?(?:\d+(?:[,\.]\d+)?)";
		public const string TextRegex = @"[a-zA-Z_]+[a-zA-Z_\d]*";
		public delegate string MathAction(string match);
		private readonly Dictionary<Regex, MathStringTemplate> _mathFunc;
		#endregion

		#region Constructeur
		public MathString()
		{
			Func<string, MathAction> action = c =>
				{
// ReSharper disable ConvertToLambdaExpression
					return s =>
// ReSharper restore ConvertToLambdaExpression
						{
							string[] vals = Regex.Split(s, "(" + NumberRegex + ")(\\" + c + ")(" + NumberRegex + ")");
							var val1 = float.Parse(vals[1].Trim()); //float.Parse(vals[0].Trim());
							var val2 = float.Parse(vals[3].Trim());
							float result = 0f;
							switch (c)
							{
								case "^":
									result = (float)Math.Pow(val1, val2);
									break;
								case "%":
									result = val1 % val2;
									break;
								case "*":
									result = val1 * val2;
									break;
								case "/":
									result = val1 / val2;
									break;
								case "+":
									result = val1 + val2;
									break;
								case "-":
									result = val1 - val2;
									break;
							}

							return result.ToString();
						};
				};

			var sym = new string[][]
				{
					new string[] {"^"},
					new string[] {"*", "/", "%"},
					new string[]{"+", "-"}
				};

			var mathSymbols = sym.SelectMany(t => t).ToDictionary(c => c, c => new Regex(NumberRegex + "( )*" + (c == "+" || c == "*" || c == "^" ? "\\" : "") + c + "( )*" + NumberRegex));
			mathSymbols.Add("number", new Regex(NumberRegex));

			_mathFunc = new Dictionary<Regex, MathStringTemplate>
				{
					{mathSymbols["number"], new MathStringTemplate("number", 0, formule => formule.Replace('.', ','))}
				};
			for (int i = 0; i < sym.Length; i++)
				foreach (string c in sym[i])
					_mathFunc.Add(mathSymbols[c], new MathStringTemplate(c, i + 1, action(c)));

			//foreach (string c in sym)
			//	_mathFunc.Add(mathSymbols[c], new MathStringTemplate(c, (c == "+" || c == "-" ? 2 : 1), action(c)));
		}
		#endregion

		#region Methodes
		public string Convert(string text)
		{
			text = text.Replace(" ", "");
			int max = _mathFunc.Values.Max(t => t.Weigth);
			var pair = _mathFunc.First(kvp => kvp.Value.Operator == "number");
			for (Match ma = pair.Key.Match(text); ma.Captures.Count != 0; ma = ma.NextMatch())
				text = text.Replace(ma.Value, pair.Value.Action(ma.Value));

			return FindParenthesis(ref text, max, '(', ')') ? text : Calculate(text, max);
		}

		private bool FindParenthesis(ref string text, int max, char open, char close)
		{
			while (text.Any(c => c == open || c == close))
			{
				int po = text.IndexOf(open);
				if (po != -1)
				{
					int pf = text.IndexOf(')', po);
					if (pf != -1)
					{
						po = text.Substring(po, pf - po).LastIndexOf(open) + po;
						po++;
						string subFormule = Calculate(text.Substring(po, pf - po), max); // Convert(text.Substring(po, pf - po));
						text = text.Substring(0, po - 1) + subFormule + text.Substring(pf + 1);
					}
					else
						return true;
				}
			}
			return false;
		}

		private string Calculate(string text, int max)
		{
			int i = 1;
			while (!Regex.IsMatch(text, "^" + NumberRegex + "$"))
			{
				char first;
				do
				{
					first =
						text.Substring(1).FirstOrDefault(
							c => _mathFunc.Values.Where(t => t.Weigth == i).FirstOrDefault(t => t.Operator == c.ToString()) != null);
				} while (first == default(int) && ++i <= max);

				var regex = _mathFunc.First(kvp => kvp.Value.Operator == first.ToString());

				Match ma = regex.Key.Match(text);
				text = text.Replace(ma.Value, regex.Value.Action(ma.Value));
			}

			return text;
		}

		#endregion
	}
}