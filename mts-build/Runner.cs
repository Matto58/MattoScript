using Mattodev.MattoScript.Engine;
using static Mattodev.MattoScript.Engine.CoreEng;
using static Mattodev.MattoScript.Engine.CoreEng.BuiltIns;
using static Mattodev.MattoScript.Engine.CoreEng.MTSFuncs;

namespace Mattodev.MattoScript.Builder
{
	public class Runner
	{
		public static Dictionary<string, MTSFunc> otherFuncs = new();
		public static Dictionary<string, (string, bool)> otherVars = new();
		public static Dictionary<string, (Int128, bool)> otherIntVars = new();

		public static int loops = 0;
		public static string inputText = "";

		internal static Dictionary<string, MTSFunc> mainFuncs = new()
		{
			{ "fima.read", new FiMa.Read() },
			{ "fima.write", new FiMa.Write() }
		};

		public static bool exit = false, input = false; // flags
		
		public static string CheckStrForVar(string str, string fileName, ref MTSConsole c)
		{
			(string, bool) vVal = c.vars.GetValueOrDefault(str, ("INTERNAL:NOVAL", true));
			(Int128, bool) vVal2 = c.intVars.GetValueOrDefault(str, (0, true));
			int vValLen = c.vars.GetValueOrDefault("$" + str[1..], ("", true)).Item1.Length;

			if (str[0] == '>')
			{
				string[] split = str[1..].Split('/');
				//c.funcs.ToList().ForEach(a => Console.WriteLine(a.Key));
				CallFunc(split[0], split, 0, fileName, ref c);
				
				if (c.returnVar != null) return c.returnVar.Value.Item1;
			}
			else if (
				((vVal.Item1 == "INTERNAL:NOVAL" || vVal.Item1 == "")
				&& str[0] == '$' && str[0] != '@')
				|| (vVal2.Item1 == Int128.Zero && str[0] == '%')
				|| (vValLen == 0 && str[0] == '@')
			)
			{
				MTSError err = new MTSError.UnassignedVar();
				err.message += str;
				err.ThrowErr(fileName, c.stopIndex, ref c);
				exit = true;
				return "";
			}
			return
				str[0] == '$' ? vVal.Item1 :
				str[0] == '%' ? vVal2.Item1.ToString() :
				str[0] == '@' ? vValLen.ToString() :
				str.Replace("\\", "");
		}

		public static bool CallFunc(string funcName, string[] ln, int off, string fileName, ref MTSConsole c)
		{
			string[] cd1 = c.funcs[funcName].Item1;
			// Console.WriteLine(strjoin(cd1, "\n") + "\n" + ln[1]);
			if (cd1[0] == "INTERNAL:NOFUNC" || cd1[0] == "")
			{
				MTSError err = new MTSError.UnassignedVar();
				err.message += $"<function {funcName}>";
				err.ThrowErr(fileName, c.stopIndex, ref c);
				exit = true;
				return false;
			}

			var newVars = c.vars;
			//Console.WriteLine($"{c.funcs[ln[1]].Item2.Length}\t{off}");
			for (int argInx = 0; argInx < ln.Length - off - 1; argInx++)
				newVars[c.funcs[funcName].Item2[argInx]] = (ln[argInx + off + 1], true);
				
			c += runFromInterLang(cd1, $"{fileName}:<function {ln[1]}>", c.vars, c.intVars, ref c);
			
			return true;
		}

		public static bool ProcessStatement(string[] strings, string filename, ref MTSConsole c)
		{
			if (loops < 1000000000)
			{
				string l = CheckStrForVar(strings[0], filename, ref c);
				string s = strings[1];
				string r = CheckStrForVar(strings[2], filename, ref c);

				string[] vs =
				{
					"eq", "neq",
					"lt", "lte",
					"gt", "gte"
				};

				if (!vs.Contains(s))
				{
					MTSError err = new MTSError.InvalidArg();
					err.message += s;
					err.ThrowErr(filename, c.stopIndex, ref c);
					c.exitCode = err.code;
					exit = true;
					return false;
				}

				return s.ToLower() switch
				{
					"eq" => l == r,
					"neq" => l != r,
					"lt" => Int128.Parse(l) < Int128.Parse(r),
					"gt" => Int128.Parse(l) > Int128.Parse(r),
					"lte" => Int128.Parse(l) >= Int128.Parse(r),
					"gte" => Int128.Parse(l) <= Int128.Parse(r),
					_ => false
				};
			}
			else
			{
				MTSError err = new MTSError.TooLongExecution();
				c.exitCode = err.code;
				exit = true;
			}
			return false;
		}

		public static MTSConsole runFromInterLang(string[] interLangLns, string fileName,
			Dictionary<string, (string, bool)> variables,
			Dictionary<string, (Int128, bool)> intVariables,
			ref MTSConsole c, bool legacyConInput = true)
		{
			#region vars
			MTSConsole varC = new();
			Dictionary<string, (string, bool)> vars = new()
			{
				{ "$ver.engine", (MTSInfo.engVer, true) },
				{ "$ver.mtscript", (MTSInfo.mtsVer, true) },
				{ "$con.title", (c.title, false) }
			};
			Dictionary<string, (Int128, bool)> intVars = new()
			{
				{ "$con.bgcolor", ((Int128)(int)Console.BackgroundColor, false) },
				{ "$con.fgcolor", ((Int128)(int)Console.ForegroundColor, false) }
			};
			Dictionary<string, (string[], string[])> funcs = new();
			List<string> func = new();
			List<string> funcArgs = new();
			string funcName = "";

			bool inFunc = false;

			c.vars = vars;
			c.intVars = intVars;

			varC.vars = variables;
			varC.intVars = intVariables;

			c.copyVars(varC);

			vars = c.vars;
			intVars = c.intVars;

			int inx = 0;
			#endregion

			foreach (string l in interLangLns)
			{
				string[] i = l.Split(";");
				string[] ln = i[1].Split(",");
				c.stopIndex = int.Parse(i[0]);
				//Console.WriteLine(l);
				MTSError err;
				MTSConsole flexC, funcC, loopC;
				string fn;

				if (exit) goto end;

				try
				{
					if (!inFunc)
					{
						loops = 0;

						// might work? (ev0.2.0.6)
						// update: kinda but similar issue to previous version (ev0.2.1.8)
						foreach (var f in otherFuncs)
							if (f.Value.interName == ln[0])
							{
								f.Value.Exec(ref c, ln, fileName, ref exit);
								continue;
							}

						foreach (var f in mainFuncs)
							if (f.Value.interName == ln[0])
							{
								f.Value.Exec(ref c, ln, fileName, ref exit);
								continue;
							}

						switch (ln[0])
						{
							case "CON:OUT":
								//Console.WriteLine("\t" + intVars[ln[2]]);
								/*string vVal = vars.GetValueOrDefault(ln[2], "INTERNAL:NOVAL");
								Int128 vVal2 = intVars.GetValueOrDefault(ln[2], 0);
								int vValLen = vars.GetValueOrDefault("$" + ln[2][1..], "").Length;
								if (
									((vVal == "INTERNAL:NOVAL" || vVal == "") && ln[2][0] == '$' && ln[2][0] != '@')
									|| (vVal2 == Int128.Zero && ln[2][0] == '%')
									|| (vValLen == Int128.Zero && ln[2][0] == '@')
								)
								{
									err = new MTSError.UnassignedVar();
									err.message += ln[2];
									err.ThrowErr("<thisfile>", c.stopIndex, ref c);
									exit = true;
								}
								else Consol3.conOut(ref c,
									ln[2][0] == '$' ? vVal :
									ln[2][0] == '%' ? vVal2.ToString() :
									ln[2][0] == '@' ? vValLen.ToString() :
									ln[2].Replace("\\", ""), ln[1] == "1"
								);*/
								//Console.WriteLine(c.cont);

								// revamped printing! (ev0.2.1.9)
								Consol3.conOut(ref c, CheckStrForVar(ln[2], fileName, ref c), ln[1] == "1");
								break;
							case "CON:INPUT":
								if (legacyConInput)
								{
									c.disp();
									c.clr();
									string? inp = Console.ReadLine();
									if (inp != null)
										vars[ln[1]] = (inp, false);
								}
								else
								{
									input = true;
									while (input); // do nothing while we're waiting for input (ev0.3.0.12)
									vars[ln[1]] = (inputText, false);
									c.cont += inputText;
								}
								break;
							case "CON:CLEAR":
								Console.Clear();
								break;
							case "INTERNAL:ERR_THROW":
								err = MTSFunc.GetErrFromName(ln[1]);
								err.message += ln[4];
								c.exitCode = err.ThrowErr(ln[2], int.Parse(ln[3]), ref c);
								exit = true;
								break;
							case "SETVAR":
								string[] eq = ln[1].Split("=");
								if (!vars.ContainsKey(eq[0]) || eq[0][1] != '!')
									vars[eq[0]] = (CheckStrForVar(eq[1], fileName, ref c), eq[0][1] == '!');
								else
								{
									err = new MTSError.VarIsConst();
									err.message += eq[0];
									err.ThrowErr(fileName, c.stopIndex, ref c);
									c.exitCode = err.code;
									exit = true;
								}
								break;

							// the FLEX module: FiLe EXecutor
							case "FLEX:EXECFL":
								fn = CheckStrForVar(ln[1], fileName, ref c);
								try
								{
									flexC = runFromCode(File.ReadAllLines(fn), fn, ref c);
									c += flexC;
								}
								catch (FileNotFoundException)
								{
									err = new MTSError.FileNotFound();
									err.message += fn;
									err.ThrowErr(fileName, c.stopIndex, ref c);
									exit = true;
								}
								break;
							case "FLEX:LOADVARS":
								fn = CheckStrForVar(ln[1], fileName, ref c);
								try
								{
									flexC = runFromCode(File.ReadAllLines(fn), fn, ref c);
									c.copyVars(flexC);
								}
								catch (FileNotFoundException)
								{
									err = new MTSError.FileNotFound();
									err.message += fn;
									err.ThrowErr(fileName, c.stopIndex, ref c);
									exit = true;
								}
								break;
							case "FLEX:LOADFUNCS":
								fn = CheckStrForVar(ln[1], fileName, ref c);
								try
								{
									flexC = runFromCode(File.ReadAllLines(fn), fn, ref c);
									c.copyFuncs(flexC);
								}
								catch (FileNotFoundException)
								{
									err = new MTSError.FileNotFound();
									err.message += fn;
									err.ThrowErr(fileName, c.stopIndex, ref c);
									exit = true;
								}
								break;

							// functions yaay!!
							case "FUNC:START":
								inFunc = true;
								funcName = ln[1];
								if (ln.Length > 2)
									funcArgs = ln[2..].ToList();
								//funcArgs.ForEach(Console.WriteLine);
								break;
							case "FUNC:CALL":
								CallFunc(ln[1], ln, 1, fileName, ref c);
								break;
							case "FUNC:RETURN":
								exit = true;
								c.returnVar = (CheckStrForVar(ln[1], fileName, ref c), false);
								break;

							// its integering time
							case "INTEGER:SETVAR":
								string[] eqv = ln[1].Split("=");
								if (!vars.ContainsKey(eqv[0]) || eqv[0][1] != '!')
									intVars[eqv[0]] = (Int128.Parse(CheckStrForVar(eqv[1], fileName, ref c)), eqv[0][1] == '!');
								else
								{
									err = new MTSError.VarIsConst();
									err.message += eqv[0];
									err.ThrowErr(fileName, c.stopIndex, ref c);
									c.exitCode = err.code;
									exit = true;
								}
								break;
							case "INTEGER:CALC":
								foreach (string val in ln[3..])
								{
									Int128 v = val[0] != '%' ? Int128.Parse(val) : intVars[val].Item1;
									switch (ln[2])
									{
										case "+":
											intVars[ln[1]] = (intVars[ln[1]].Item1 + v, false);
											break;
										case "-":
											intVars[ln[1]] = (intVars[ln[1]].Item1 - v, false);
											break;
										case "*":
											intVars[ln[1]] = (intVars[ln[1]].Item1 * v, false);
											break;
										case "/":
											intVars[ln[1]] = (intVars[ln[1]].Item1 / v, false);
											break;
										case "^":
											intVars[ln[1]] = ((Int128)Math.Pow((double)intVars[ln[1]].Item1, (double)v), false);
											break;
										default:
											err = new MTSError.InvalidArg();
											err.message += ln[2];
											err.ThrowErr(fileName, c.stopIndex, ref c);
											exit = true;
											break;
									}
								}
								break;

							case "LOOP:FOR":
								string[] finp = ln[1].Split('=');
								string[] fvals = finp[1].Split(':');
								Int128[] fvals2 = new Int128[]
								{
									Int128.Parse(fvals[0]),
									Int128.Parse(fvals[1])
								};
								for (Int128 fi = fvals2[0]; fi < fvals2[1]; fi += fvals.Length < 3 ? 1 : Int128.Parse(fvals[2]))
								{
									var loopVars = intVars;
									loopVars[finp[0]] = (fi, true);
									//Console.WriteLine(finp[0] + "\t" + loopVars[finp[0]]);
									loopC = runFromInterLang(funcs[ln[2]].Item1, $"{fileName}:<forloop>:<function {ln[2]}>", vars, loopVars, ref c, legacyConInput);
									c += loopC;
									loops++;
								}
								break;
							case "COND:IF":
								if (ProcessStatement(ln[1..], fileName, ref c))
								{
									string[] cd2 = funcs[ln[4]].Item1;
									// Console.WriteLine(strjoin(cd1, "\n") + "\n" + ln[1]);
									if (cd2[0] == "INTERNAL:NOFUNC" || cd2[0] == "")
									{
										err = new MTSError.UnassignedVar();
										err.message += $"<function {ln[4]}>";
										err.ThrowErr(fileName, c.stopIndex, ref c);
										exit = true;
									}
									else
									{
										funcC = runFromInterLang(cd2, $"{fileName}:<function {ln[4]}>", c.vars, c.intVars, ref c, legacyConInput);
										c += funcC;
									}
									break;
								}
								break;
							case "LOOP:WHILE":
								while (ProcessStatement(ln[1..], fileName, ref c))
								{
									CallFunc(ln[4], ln, 4, fileName, ref c);
									loops++;
								}
								break;
						}
						c.vars = vars;
						c.intVars = intVars;
						c.funcs = funcs;
					}
					else
					{
						if (ln[0] != "FUNC:END")
						{
							func.Add(l);
						}
						else
						{
							inFunc = false;
							funcs[funcName] = (func.ToArray(), funcArgs.ToArray());
							funcName = "";
							func = new();
							funcArgs = new();
						}
					}

					inx++;
					c.title = vars["$con.title"].Item1;
					Console.Title = c.title;
					Console.ForegroundColor = (ConsoleColor)(int)intVars["$con.fgcolor"].Item1;
					Console.BackgroundColor = (ConsoleColor)(int)intVars["$con.bgcolor"].Item1;
				}
				catch (Exception e)
				{
					err = new MTSError.InternalError(e);
					err.message += e.Message;
					err.ThrowErr(fileName, c.stopIndex, ref c);
					c.exitCode = err.code;
					exit = true;
				}
			}
			end: return c;
		}
		public static MTSConsole runFromInterLang(string interLangCode, string fileName,
			Dictionary<string, (string, bool)> variables,
			Dictionary<string, (Int128, bool)> intVariables,
			ref MTSConsole c, bool legacyConInput = true)
			=> runFromInterLang(interLangCode.ReplaceLineEndings("\n").Split("\n"), fileName, variables, intVariables, ref c, legacyConInput);

		public static MTSConsole runFromCode(string[] lns, string fileName, ref MTSConsole c, bool legacyConInput = true)
			=> runFromInterLang(InterLang.toInterLang(lns, fileName), fileName, otherVars, otherIntVars, ref c, legacyConInput);
		public static MTSConsole runFromCode(string code, string fileName, ref MTSConsole c, bool legacyConInput = true)
			=> runFromCode(code.ReplaceLineEndings("\n").Split("\n"), fileName, ref c, legacyConInput);
	}
}
