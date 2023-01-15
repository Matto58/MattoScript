using Mattodev.MattoScript.Engine;
using static Mattodev.MattoScript.Engine.CoreEng;
using static Mattodev.MattoScript.Engine.CoreEng.BuiltIns;
using static Mattodev.MattoScript.Engine.CoreEng.MTSFuncs;

namespace Mattodev.MattoScript.Builder
{
	public class Runner
	{
		public static Dictionary<string, MTSFunc> otherFuncs = new();
		public static Dictionary<string, string> otherVars = new();
		public static Dictionary<string, Int128> otherIntVars = new();

		internal static Dictionary<string, MTSFunc> mainFuncs = new()
		{
			{ "fima.read", new FiMa.Read() },
			{ "fima.write", new FiMa.Write() }
		};

		public static bool exit = false;
		
		public static string CheckStrForVar(string str, ref MTSConsole c, Dictionary<string, string> vars, Dictionary<string, Int128> intVars)
		{
			string vVal = vars.GetValueOrDefault(str, "INTERNAL:NOVAL");
			Int128 vVal2 = intVars.GetValueOrDefault(str, 0);
			int vValLen = vars.GetValueOrDefault("$" + str[1..], "").Length;
			if (
				((vVal == "INTERNAL:NOVAL" || vVal == "")
				&& str[0] == '$' && str[0] != '@')
				|| (vVal2 == Int128.Zero && str[0] == '%')
				|| (vValLen == Int128.Zero && str[0] == '@')
			)
			{
				MTSError err = new MTSError.UnassignedVar();
				err.message += str;
				err.ThrowErr("<thisfile>", c.stopIndex, ref c);
				exit = true;
				return "";
			}
			return
				str[0] == '$' ? vVal :
				str[0] == '%' ? vVal2.ToString() :
				str[0] == '@' ? vValLen.ToString() :
				str.Replace("\\", "");
		}

		public static bool ProcessStatement(string[] strings, string filename, int i, ref MTSConsole c, Dictionary<string, string> vars, Dictionary<string, Int128> intVars)
		{
			string l = CheckStrForVar(strings[0], ref c, vars, intVars);
			string s = strings[1];
			string r = CheckStrForVar(strings[2], ref c, vars, intVars);

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
				err.ThrowErr(filename, i, ref c);
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

		public static MTSConsole runFromInterLang(string[] interLangLns, string fileName, Dictionary<string, string> variables, Dictionary<string, Int128> intVariables)
		{
			MTSConsole c = new(), varC = new();
			Dictionary<string, string> vars = new()
			{
				{ "$ver.engine", MTSInfo.engVer },
				{ "$ver.mtscript", MTSInfo.mtsVer },
				{ "$con.title", c.title }
			};
			Dictionary<string, Int128> intVars = new()
			{
				{ "$con.bgcolor", (Int128)(int)Console.BackgroundColor },
				{ "$con.fgcolor", (Int128)(int)Console.ForegroundColor }
			};
			Dictionary<string, string[]> funcs = new();
			List<string> func = new();
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
								Consol3.conOut(ref c, CheckStrForVar(ln[2], ref c, vars, intVars), ln[1] != "1");
								break;
							case "CON:INPUT":
								c.disp();
								c.clr();
								string? inp = Console.ReadLine();
								if (inp != null)
									vars[ln[1]] = inp;
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
								vars[eq[0]] = CheckStrForVar(eq[1], ref c, vars, intVars);
								break;

							// the FLEX module: FiLe EXecutor
							case "FLEX:EXECFL":
								fn = CheckStrForVar(ln[1], ref c, vars, intVars);
								try
								{
									flexC = runFromCode(File.ReadAllLines(fn), fn);
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
								fn = CheckStrForVar(ln[1], ref c, vars, intVars);
								try
								{
									flexC = runFromCode(File.ReadAllLines(fn), fn);
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
								fn = CheckStrForVar(ln[1], ref c, vars, intVars);
								try
								{
									flexC = runFromCode(File.ReadAllLines(fn), fn);
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
								break;
							case "FUNC:CALL":
								string[] cd1 = funcs[ln[1]];
								// Console.WriteLine(strjoin(cd1, "\n") + "\n" + ln[1]);
								if (cd1[0] == "INTERNAL:NOFUNC" || cd1[0] == "")
								{
									err = new MTSError.UnassignedVar();
									err.message += $"<function {ln[1]}>";
									err.ThrowErr(fileName, c.stopIndex, ref c);
									exit = true;
								}
								else
								{
									funcC = runFromInterLang(cd1, $"{fileName}:<function {ln[1]}>", c.vars, c.intVars);
									c += funcC;
								}
								break;

							// its integering time
							case "INTEGER:SETVAR":
								string[] eqv = ln[1].Split("=");
								fn = CheckStrForVar(eqv[1], ref c, vars, intVars);
								intVars[eqv[0]] = Int128.Parse(fn);
								break;
							case "INTEGER:CALC":
								foreach (string val in ln[3..])
								{
									Int128 v = val[0] != '%' ? Int128.Parse(val) : intVars[val];
									switch (ln[2])
									{
										case "+":
											intVars[ln[1]] += v;
											break;
										case "-":
											intVars[ln[1]] -= v;
											break;
										case "*":
											intVars[ln[1]] *= v;
											break;
										case "/":
											intVars[ln[1]] /= v;
											break;
										case "^":
											intVars[ln[1]] = (Int128)Math.Pow((double)intVars[ln[1]], (double)v);
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
									Dictionary<string, Int128> loopVars = intVars;
									loopVars[finp[0]] = fi;
									//Console.WriteLine(finp[0] + "\t" + loopVars[finp[0]]);
									loopC = runFromInterLang(funcs[ln[2]], $"{fileName}:<forloop>:<function {ln[2]}>", vars, loopVars);
									c += loopC;
								}
								break;
							case "COND:IF":
								if (ProcessStatement(ln[1..], fileName, inx, ref c, vars, intVars))
								{
									fn = CheckStrForVar(ln[4], ref c, vars, intVars);
									try
									{
										flexC = runFromCode(File.ReadAllLines(fn), fn);
										c += flexC;
									}
									catch (FileNotFoundException)
									{
										err = new MTSError.FileNotFound();
										err.message += fn;
										err.ThrowErr(fileName, c.stopIndex, ref c);
										exit = true;
									}
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
							funcs[funcName] = func.ToArray();
							funcName = "";
							func = new();
						}
					}

					inx++;
					c.title = vars["$con.title"];
					Console.Title = c.title;
					Console.ForegroundColor = (ConsoleColor)(int)intVars["$con.fgcolor"];
					Console.BackgroundColor = (ConsoleColor)(int)intVars["$con.bgcolor"];
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
		public static MTSConsole runFromInterLang(string interLangCode, string fileName, Dictionary<string,string> variables, Dictionary<string, Int128> intVariables)
			=> runFromInterLang(interLangCode.ReplaceLineEndings("\n").Split("\n"), fileName, variables, intVariables);

		public static MTSConsole runFromCode(string[] lns, string fileName)
			=> runFromInterLang(InterLang.toInterLang(lns, fileName), fileName, otherVars, otherIntVars);
		public static MTSConsole runFromCode(string code, string fileName)
			=> runFromCode(code.ReplaceLineEndings("\n").Split("\n"), fileName);
	}
}