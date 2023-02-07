using System.Diagnostics;
using Mattodev.MattoScript.Engine;
using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
    internal class Program
    {
        static int Main(string[] args)
        {
            MTSConsole c = new();
            Console.Title = c.title;

			Stopwatch s = new();
			s.Start();
			if (args.Length == 0)
            {
                MTSError.TooLittleArgs err = new();
                err.message += args.Length;
                err.ThrowErr("<build>", -1, ref c);
                c.exitCode = err.code;
                goto end;
            }
            string[] code;
            try
            {
                code = File.ReadAllLines(args[0]);
            }
            catch (FileNotFoundException)
            {
                MTSError.FileNotFound err = new();
                err.message += args[0];
                err.ThrowErr("<build>", -1, ref c);
                c.exitCode = err.code;
                goto end;
            }
            catch (Exception e)
            {
                MTSError.InternalError err = new(e);
                err.message += $"\n\t{e.Message}";
                err.ThrowErr("<build>", -1, ref c);
                c.exitCode = err.code;
                goto end;
            }

            Runner.runFromCode(code, args[0], ref c);

            end:
			s.Stop();
			c.disp();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"(exit code {c.exitCode}, execution time {s.ElapsedMilliseconds}ms)");
            Console.ResetColor();
            return c.exitCode;
        }
    }
}