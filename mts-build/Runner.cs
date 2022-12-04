using Mattodev.MattoScript.Engine;
using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
    public class Runner
    {
        public static MTSConsole runFromInterLang(string[] interLangLns)
        {
            MTSConsole c = new();
            List<MTSVar> vars = new();
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
                        c.cont += (ln[2][0] != "$"[0] ? ln[2] : MTSVar.search(vars, ln[2][1..])) + (ln[1] == "1" ? "\n" : "");
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
                        vars.Add(new(ln[1], ln[2]));
                        break;
                }
            }
            end: return c;
        }

        public static MTSConsole runFromCode(string[] lns, string fileName)
            => runFromInterLang(InterLang.toInterLang(lns, fileName));
        public static MTSConsole runFromCode(string code, string fileName)
            => runFromCode(code.ReplaceLineEndings("\n").Split("\n"), fileName);
    }
}