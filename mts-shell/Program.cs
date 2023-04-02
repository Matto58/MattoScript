using static Mattodev.MattoScript.Engine.CoreEng;
using Mattodev.MattoScript.Engine;
using Mattodev.MattoScript.Builder;

using System.Diagnostics;

namespace Mattodev.MattoScript.Shell
{
	internal class Program
	{
		static int Main(string[] args)
		{
			bool debug = false;
			MTSConsole con = new();
			Console.WriteLine($"MattoScript v{MTSInfo.mtsVer} (engine version {MTSInfo.engVer}) - shell");
			while (true)
			{
				Console.Title = con.title;
				Console.Write(">");
				string? i = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(i))
				{
					string[] ln = i.Split(" ");

					con = new();
					MTSError err;
					Stopwatch s = new();
					s.Start();
					switch (ln[0].ToLower())
					{
						case "debug":
							debug = !debug;
							break;
						case "exit": goto end;
						case "tointerf":
							try
							{
								string[] code = InterLang.toInterLang(File.ReadAllText(ln[1]), ln[1]);
								Console.WriteLine(strjoin(code, "\n"));
							}
							catch (IndexOutOfRangeException)
							{
								err = new MTSError.TooLittleArgs();
								err.message += $"{ln.Length-1}";
								err.ThrowErr("<shell>", -1, ref con);
							}
							break;
						case "readf":
							try
							{
								Console.WriteLine(File.ReadAllText(ln[1]));
							}
							catch (IndexOutOfRangeException)
							{
								err = new MTSError.TooLittleArgs();
								err.message += $"{ln.Length - 1}";
								err.ThrowErr("<shell>", -1, ref con);
							}
							catch (FileNotFoundException)
							{
								err = new MTSError.FileNotFound();
								err.message += ln[1];
								err.ThrowErr("<shell>", -1, ref con);
							}
							break;
						case "execf":
							try
							{
								Runner.runFromCode(File.ReadAllText(ln[1]), ln[1], ref con);
							}
							catch (IndexOutOfRangeException)
							{
								err = new MTSError.TooLittleArgs();
								err.message += $"{ln.Length - 1}";
								try { err.ThrowErr(ln[1], con.stopIndex, ref con); } // the toolittleargs error is the mts file's fault - too little args in code
								catch { err.ThrowErr("<shell>", -1, ref con); } // the toolittleargs error is the shell's fault - too little args in shell input
							}
							catch (FileNotFoundException)
							{
								err = new MTSError.FileNotFound();
								err.message += ln[1];
								err.ThrowErr("<shell>", -1, ref con);
							}
							break;
						case "execfi":
							try
							{
								Runner.runFromInterLang(File.ReadAllText(ln[1]), ln[1], Runner.otherVars, Runner.otherIntVars, Runner.otherEnums, ref con);
							}
							catch (IndexOutOfRangeException)
							{
								err = new MTSError.TooLittleArgs();
								err.message += $"{ln.Length - 1}";
								try { err.ThrowErr(ln[1], con.stopIndex, ref con); } // the toolittleargs error is the mts file's fault - too little args in code
								catch { err.ThrowErr("<shell>", -1, ref con); } // the toolittleargs error is the shell's fault - too little args in shell input
							}
							catch (FileNotFoundException)
							{
								err = new MTSError.FileNotFound();
								err.message += ln[1];
								err.ThrowErr("<shell>", -1, ref con);
							}
							break;
						case "execln":
							try
							{
								//Console.WriteLine(String.Join(' ', ln[1..]));
								Runner.runFromInterLang(string.Join(' ', ln[1..]), "<shell>", Runner.otherVars, Runner.otherIntVars, Runner.otherEnums, ref con);
							}
							catch (IndexOutOfRangeException)
							{
								err = new MTSError.TooLittleArgs();
								err.message += $"{ln.Length - 1}";
								try { err.ThrowErr("<shell>", con.stopIndex, ref con); } // the toolittleargs error is the mts file's fault - too little args in code
								catch { err.ThrowErr("<shell>", -1, ref con); } // the toolittleargs error is the shell's fault - too little args in shell input
							}
							break;
						case "tointerf_save":
							try
							{
								string[] code = InterLang.toInterLang(File.ReadAllText(ln[1]), ln[1]);
								File.WriteAllText($"{ln[1].Split(".")[0]}.mti", strjoin(code, "\n"));
							}
							catch (IndexOutOfRangeException)
							{
								err = new MTSError.TooLittleArgs();
								err.message += $"{ln.Length - 1}";
								err.ThrowErr("<shell>", -1, ref con);
							}
							break;
						default:
							err = new MTSError.InvalidCommand();
							err.message += ln[0];
							err.ThrowErr("<shell>", -1, ref con);
							break;
					}
					s.Stop();
					con.cont += "\n";
					con.disp();
					if (debug) Console.WriteLine($"(took {Math.Round(s.Elapsed.TotalMilliseconds, 1)}ms)");
				}
			}
			end: return 0;
		}
	}
}