using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using CSharpHelper;

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

		public class Variable: IEqualityComparer<Variable>
		{
			#region Attributs

			private readonly string _name;
			private readonly float _value;

			#endregion

			#region Constructeur

			public Variable(string name, float value)
			{
				_name = name;
				_value = value;
			}

			#endregion

			#region Proprietes

			public string Name
			{
				get { return _name; }
			}

			public float Value
			{
				get { return _value; }
			}

			#endregion

			/// <summary>
			/// Détermine si les objets spécifiés sont égaux.
			/// </summary>
			/// <returns>
			/// true si les objets spécifiés sont égaux ; sinon, false.
			/// </returns>
			/// <param name="x">Premier objet de type <paramref name="Variable"/> à comparer.</param>
			/// <param name="y">Deuxième objet de type <paramref name="Variable"/> à comparer.</param>
			public bool Equals(Variable x, Variable y)
			{
				return x.Name == y.Name;
			}

			/// <summary>
			/// Retourne un code de hachage pour l'objet spécifié.
			/// </summary>
			/// <returns>
			/// Code de hachage pour l'objet spécifié.
			/// </returns>
			/// <param name="obj"><see cref="T:Variable"/> pour lequel un code de hachage doit être retourné.</param>
			/// <exception cref="T:System.ArgumentNullException">Le type de <paramref name="obj"/> est un type référence et <paramref name="obj"/> est null.</exception>
			public int GetHashCode(Variable obj)
			{
				return (obj.Name.GetHashCode() + obj.Value.GetHashCode()).GetHashCode();
			}
		}
		#endregion

		#region Attributs
		public const string NumberRegex = @"-?(?:\d+(?:\.\d+)?)";
		public const string DecimalRegex = @"-?(?:\d+(?:[,\.]\d+)?)";
		public const string TextRegex = @"[a-zA-Z_]+[_\w]*";
		public const string VariableRegex = @"\$(?<nom>"+TextRegex+");";
		public const string FunctionRegex = @"\@(?<nom>" + TextRegex + @") ?\((?<param1>" + NumberRegex + @")?(?:, ?(?<autres>" + NumberRegex + @"))*\)";
		
		public delegate string MathAction(string match);
		public delegate float Function(params float[] values);
		
		private readonly Dictionary<Regex, MathStringTemplate> _mathFunc;

		private readonly Dictionary<Variable, string> _variables;
		private static readonly Dictionary<Variable, string> Variables;
		private readonly Dictionary<string, Function> _functions;
		private static readonly Dictionary<string, Function> Functions;
		#endregion

		#region Constructeur
		static MathString()
		{
			Random rnd = new Random();
			Variables = new Dictionary<Variable, string>();
			Functions = new Dictionary<string, Function>
				#region Functions

				{
					{
						"random", v =>
							{
								if (v.Length == 0)
									return (float)rnd.NextDouble();
								if (v.Length == 1)
									return rnd.Next((int)v[0]);
								if (v.Length == 2)
									return rnd.Next((int)v[0], (int)v[1]);
								throw new Exception("usage: @Random([min[, max]])");
							}
					},
					{
						"abs", v =>
							{
								if (v.Length == 1)
									if (v[0] < 0)
										return -v[0];
									else
										return v[0];
								throw new Exception("usage: @Abs(<value>)");
							}
					}
				};

			#endregion

		}

		public MathString()
		{
			_variables = new Dictionary<Variable, string>();
			_functions = new Dictionary<string, Function>();

			Func<string, MathAction> action = c =>
				{
// ReSharper disable ConvertToLambdaExpression
					return s =>
// ReSharper restore ConvertToLambdaExpression
						{
							var vals = Regex.Split(s, "(" + NumberRegex + ")(\\" + c + ")(" + NumberRegex + ")");
							var val1 = float.Parse(vals[1].Replace('.', ',').Trim());
							var val2 = float.Parse(vals[3].Replace('.', ',').Trim());
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

							return result.ToString("F");
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
		}
		#endregion

		#region Methodes
		public void AddVariable(Variable var)
		{
			if (_variables.ContainsKey(var))
				_variables[var] = var.Value.ToString();
			else
				_variables.Add(var, var.Value.ToString());
		}

		public void AddVariables(IEnumerable<Variable> vars)
		{
			foreach (Variable variable in vars)
				AddVariable(variable);
		}

		public static void AddGlobalVariable(Variable var)
		{
			if (Variables.ContainsKey(var))
				Variables[var] = var.Value.ToString();
			else
				Variables.Add(var, var.Value.ToString());
		}

		public static void AddGlobalVariables(IEnumerable<Variable> vars)
		{
			foreach (Variable variable in vars)
				AddGlobalVariable(variable);
		}

		public void AddFunction(string name, Function function)
		{
			if (_functions.ContainsKey(name))
				throw new Exception("Cette fonction existe deja: " + name);
			_functions.Add(name, function);
		}

		public static void AddGlobalVariable(string name, Function function)
		{
			if (Functions.ContainsKey(name))
				throw new Exception("Cette fonction existe deja: " + name);
			Functions.Add(name, function);
		}

		public string Convert(string text, params Variable[] variables)
		{
			text = text.Replace(" ", "");
			int max = _mathFunc.Values.Max(t => t.Weigth);
			var pair = _mathFunc.First(kvp => kvp.Value.Operator == "number");
			for (Match ma = pair.Key.Match(text); ma.Success; ma = ma.NextMatch())
				text = text.Replace(ma.Value, pair.Value.Action(ma.Value));

			text = FindVariables(text, variables);

			text = FindFunctions(text);

			return FindParenthesis(ref text, max, '(', ')') ? text : Calculate(text, max);
		}

		private string FindVariables(string text, ICollection<Variable> variables)
		{
			Regex varRegex = new Regex(VariableRegex);
			IList<string> errors = new List<string>();
			bool cont;

			for (var ma = varRegex.Match(text); ma.Success; ma = (cont) ? ma.NextMatch() : varRegex.Match(text))
			{
				Variable variable = null;

				if (variables.Count > 0)
					variable = variables.FirstOrDefault(v => v.Name == ma.Groups["nom"].Value.ToLower());

				variable = variable ??
						   (_variables.Keys.FirstOrDefault(v => v.Name == ma.Groups["nom"].Value.ToLower()) ??
							Variables.Keys.FirstOrDefault(v => v.Name == ma.Groups["nom"].Value.ToLower()));

				if (variable != null)
				{
					text = text.Replace(ma.Value, variable.Value.ToString().Replace(',', '.'));
					cont = false;
				}
				else
				{
					errors.Add(string.Format("{0}: c({1})", ma.Groups["nom"].Value, ma.Index));
					cont = true;
				}
			}
			if (errors.Count > 0)
				throw new Exception(string.Format("Impossible de trouver {0} variable{1} : {2}",
												  errors.Count == 1 ? "la" : "les",
												  errors.Count > 1 ? "s" : "",
												  errors.Join(", ")));
			return text;
		}

		private string FindFunctions(string text)
		{
			Regex funcRegex = new Regex(FunctionRegex);
			IList<string> errors = new List<string>();
			bool cont;

			for (var ma = funcRegex.Match(text); ma.Success; ma = (cont) ? ma.NextMatch() : funcRegex.Match(text))
			{
				var func = _functions.FirstOrDefault(f => f.Key == ma.Groups["nom"].Value.ToLower());
				if (func.Equals(default(KeyValuePair<string, Function>)))
					func = Functions.FirstOrDefault(f => f.Key == ma.Groups["nom"].Value.ToLower());

				if (!func.Equals(default(KeyValuePair<string, Function>)))
				{
					List<float> values = new List<float>();
					if (ma.Groups["param1"].Success)
					{
						values.Add(float.Parse(ma.Groups["param1"].Value.Replace('.', ',').Trim()));
						values.AddRange(from Capture capture in ma.Groups["autres"].Captures
										select float.Parse(capture.Value.Replace('.', ',').Trim()));
					}

					text = text.Replace(ma.Value, func.Value(values.ToArray()).ToString().Replace(',', '.'));

					cont = false;
				}
				else
				{
					errors.Add(string.Format("{0}: c({1})", ma.Groups["nom"].Value, ma.Index));
					cont = true;
				}
			}
			if (errors.Count > 0)
				throw new Exception(string.Format("Impossible de trouver {0} fonction{1} : {2}",
												  errors.Count == 1 ? "la" : "les",
												  errors.Count > 1 ? "s" : "",
												  errors.Join(", ")));
			return text;
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
						string subFormule = Calculate(text.Substring(po, pf - po), max);
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
				text = text.Replace(ma.Value, regex.Value.Action(ma.Value)).Replace(',', '.');
			}

			return text;
		}

		#endregion
	}
}