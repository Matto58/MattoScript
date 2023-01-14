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

            c = Runner.runFromCode(code, args[0]);

            end:
            c.disp();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"(exit code {c.exitCode})");
            Console.ResetColor();
            return c.exitCode;
        }
    }
}