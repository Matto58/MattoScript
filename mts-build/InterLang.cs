using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
	public class InterLang
	{
		public static string[] toInterLang(string[] lns, string fileName)
		{
			List<string> oc = new();
			Dictionary<string, MTSFunc> otherFuncs = Runner.otherFuncs;
			Dictionary<string, MTSFunc> mainFuncs  = Runner.mainFuncs;
			for (int i = 0; i < lns.Length; i++)
			{
				string l = lns[i].Replace("\t", "").Split("#")[0];
				if (!string.IsNullOrWhiteSpace(l))
				{
					string[] ln = l.Split(" ");
					//Console.WriteLine("'" + ln[0] + "'");

					// might work? (ev0.2.0.6)
					// update: no, but it should now (ev0.2.0.7)
					foreach (var f in otherFuncs)
						if (f.Key == ln[0])
						{
							oc.Add(f.Value.ToInterlang(i, ln, fileName));
							continue;
						}

					foreach (var f in mainFuncs)
						if (f.Key == ln[0])
						{
							oc.Add(f.Value.ToInterlang(i, ln, fileName));
							continue;
						}

					switch (ln[0])
					{
						case "con.out":
							try
							{
								oc.Add($"{i};CON:OUT,{(ln[1].ToLower() == "nl" ? "1" : "0")},{strjoin(ln[2..], " ")}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;
						// update: variable assignment fixed in engine 0.1.0.2
						case "var":
							string[] v;
							try
							{
								v = strjoin(ln[1..], " ").Split("=");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,NoVarVal,{fileName},{i},{ln[1]}");
								goto end;
							}
							try
							{
								oc.Add($"{i};SETVAR,{v[0]}={v[1]}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length},{ln[0]}");
								goto end;
							}
							break;
						case "halt": goto end;
						case "nop": break;
						case "throw":
							try
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,{strjoin(ln[1..])}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;
						case "con.input":
							try
							{
								oc.Add($"{i};CON:INPUT,{ln[1]}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;
						case "clear" or "con.clear":
							oc.Add($"{i};CON:CLEAR");
							break;

						// the FLEX module: FiLe EXecutor
						case "flex.file.exec":
							try
							{
								oc.Add($"{i};FLEX:EXECFL,{strjoin(ln[1..], " ")}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;
						case "flex.file.loadVars":
							try
							{
								oc.Add($"{i};FLEX:LOADVARS,{strjoin(ln[1..], " ")}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;
						case "flex.file.loadFuncs":
							try
							{
								oc.Add($"{i};FLEX:LOADFUNCS,{strjoin(ln[1..], " ")}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;

						// oh boy its time for functions in mattoscript!
						case "func.start":
							try
							{
								oc.Add($"{i};FUNC:START,{ln[1]}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;
						case "func.end":
							oc.Add($"{i};FUNC:END");
							break;
						case "func.call" or "call":
							try
							{
								oc.Add($"{i};FUNC:CALL,{ln[1]}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;

						case "for" or "loop.for":
							try
							{
								oc.Add($"{i};LOOP:FOR,{ln[1]},{ln[2]}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
								goto end;
							}
							break;

						// you know what mattoscript really needs? INTEGERS (ev0.2.0.6)
						case "int.var":
							string[] iv;
							Int128 ivv;
							try
							{
								iv = strjoin(ln[1..], " ").Split("=");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,NoVarVal,{fileName},{i},{ln[1]}");
								goto end;
							}
							try
							{
								bool parseAtt = Int128.TryParse(iv[1], out ivv);
								if (parseAtt) oc.Add($"{i};INTEGER:SETVAR,{iv[0]}={ivv}");
								else
								{
									oc.Add($"{i};INTERNAL:ERR_THROW,InvalidInt,{fileName},{i},{iv[1]},{ln[0]}");
									goto end;
								}
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length},{ln[0]}");
								goto end;
							}
							break;
						case "calc":
							try
							{
								// lineNum;INTEGER:CALC,varName,mathSymbol,[numbers,...]
								oc.Add($"{i};INTEGER:CALC,{ln[1]},{ln[2]},{strjoin(ln[3..])}");
							}
							catch (IndexOutOfRangeException)
							{
								oc.Add($"{i};INTERNAL:ERR_THROW,NoVarVal,{fileName},{i},{ln[1]}");
								goto end;
							}
							break;
						default:
							oc.Add($"{i};INTERNAL:ERR_THROW,InvalidCommand,{fileName},{i},{ln[0]}");
							goto end;
					}
				}
			}
			//Console.WriteLine(oc.Count);
			end: return oc.ToArray();
		}
		public static string[] toInterLang(string code, string fileName)
			=> toInterLang(code.ReplaceLineEndings("\n").Split("\n"), fileName);
	}
}