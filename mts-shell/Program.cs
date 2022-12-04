using static Mattodev.MattoScript.Engine.CoreEng;
using Mattodev.MattoScript.Engine;
using Mattodev.MattoScript.Builder;

namespace Mattodev.MattoScript.Shell
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine($"MattoScript v{MTSInfo.mtsVer} (engine version {MTSInfo.engVer}) - shell");
            while (true)
            {
                Console.Write(">");
                string? i = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(i))
                {
                    string[] ln = i.Split(" ");

                    MTSError err;
                    MTSConsole con = new();
                    switch (ln[0])
                    {
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
                                con = Runner.runFromCode(File.ReadAllText(ln[1]), ln[1]);
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
                        default:
                            err = new MTSError.InvalidCommand();
                            err.message += ln[0];
                            err.ThrowErr("<shell>", -1, ref con);
                            break;
                    }
                    Console.WriteLine(con.cont);
                }
            }
            end: return 0;
        }
    }
}