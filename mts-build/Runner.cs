using Mattodev.MattoScript.Engine;
using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
    public class Runner
    {
        public static MTSConsole runFromInterLang(string[] interLangLns)
        {
            MTSConsole c = new();
            Dictionary<string, string> vars = new();
            foreach (string l in interLangLns)
            {
                string[] i = l.Split(";");
                string[] ln = i[1].Split(",");
                c.stopIndex = int.Parse(i[0]);
                //Console.WriteLine(l);
                MTSError err;
                switch (ln[0])
                {
                    case "CON:OUT":
                        string vVal = vars.GetValueOrDefault(ln[2], "INTERNAL:NOVAL");
                        if (vVal == "INTERNAL:NOVAL" && ln[2][0] == "$"[0])
                        {
                            err = new MTSError.UnassignedVar();
                            err.message += ln[2];
                            err.ThrowErr("<thisfile>", c.stopIndex, ref c);
                        }
                        else c.cont += (ln[2][0] != "$"[0] ? ln[2] : vVal) + (ln[1] == "1" ? "\n" : "");
                        //Console.WriteLine(c.cont);
                        break;
                    case "INTERNAL:ERR_THROW":
                        err = ln[1] switch
                        {
                            "InvalidCommand" => new MTSError.InvalidCommand(),
                            "TooLittleArgs" => new MTSError.TooLittleArgs(),
                            "FileNotFound" => new MTSError.FileNotFound(),
                            _ => new(),
                        };
                        err.message += ln[4];
                        c.exitCode = err.ThrowErr(ln[2], int.Parse(ln[3]), ref c);
                        goto end;
                    case "SETVAR":
                        string[] eq = ln[1].Split("=");
                        vars.Add(eq[0], eq[1]);
                        break;
                }
            }
            end: return c;
        }
        public static MTSConsole runFromInterLang(string interLangCode)
            => runFromInterLang(interLangCode.ReplaceLineEndings("\n").Split("\n"));

        public static MTSConsole runFromCode(string[] lns, string fileName)
            => runFromInterLang(InterLang.toInterLang(lns, fileName));
        public static MTSConsole runFromCode(string code, string fileName)
            => runFromCode(code.ReplaceLineEndings("\n").Split("\n"), fileName);
    }
}