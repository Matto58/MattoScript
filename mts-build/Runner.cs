using Mattodev.MattoScript.Engine;
using System.Numerics;
using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
    public class Runner
    {
        public static MTSConsole runFromInterLang(string[] interLangLns)
        {
            MTSConsole c = new();
            Dictionary<string, string> vars = new()
            {
                { "ver.engine", MTSInfo.engVer },
                { "ver.mtscript", MTSInfo.mtsVer }
            };
            foreach (string l in interLangLns)
            {
                string[] i = l.Split(";");
                string[] ln = i[1].Split(",");
                c.stopIndex = int.Parse(i[0]);
                //Console.WriteLine(l);
                MTSError err;
                try
                {
                    switch (ln[0])
                    {
                        case "CON:OUT":
                            string vVal = vars.GetValueOrDefault(ln[2], "INTERNAL:NOVAL");
                            if ((vVal == "INTERNAL:NOVAL" || vVal == "") && ln[2][0] == "$"[0])
                            {
                                err = new MTSError.UnassignedVar();
                                err.message += ln[2];
                                err.ThrowErr("<thisfile>", c.stopIndex, ref c);
                                goto end;
                            }
                            else BuiltIns.Console.conOut(ref c, (ln[2][0] != "$"[0] ? ln[2] : vVal), ln[1] == "1");
                            //Console.WriteLine(c.cont);
                            break;
                        case "CON:INPUT":
                            c.disp();
                            c.clr();
                            string? inp = Console.ReadLine();
                            if (inp != null)
                                vars[ln[1]] = inp;
                            break;
                        case "INTERNAL:ERR_THROW":
                            err = ln[1] switch
                            {
                                "InvalidCommand" => new MTSError.InvalidCommand(),
                                "TooLittleArgs" => new MTSError.TooLittleArgs(),
                                "FileNotFound" => new MTSError.FileNotFound(),
                                "UnassignedVar" => new MTSError.UnassignedVar(),
                                "NoVarVal" => new MTSError.NoVarVal(),
                                _ => new(),
                            };
                            err.message += ln[4];
                            c.exitCode = err.ThrowErr(ln[2], int.Parse(ln[3]), ref c);
                            goto end;
                        case "SETVAR":
                            string[] eq = ln[1].Split("=");
                            vars[eq[0]] = eq[1];
                            break;
                    }
                }
                catch (Exception e)
                {
                    err = new MTSError.InternalError(e);
                    err.message += e.Message;
                    err.ThrowErr("<internal>", -1, ref c);
                    c.exitCode = err.code;
                    goto end;
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