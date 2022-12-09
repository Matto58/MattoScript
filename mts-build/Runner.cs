﻿using Mattodev.MattoScript.Engine;
using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
    public class Runner
    {
        public static MTSConsole runFromInterLang(string[] interLangLns, string fileName)
        {
            MTSConsole c = new();
            Dictionary<string, string> vars = new()
            {
                { "$ver.engine", MTSInfo.engVer },
                { "$ver.mtscript", MTSInfo.mtsVer }
            };
            Dictionary<string, string[]> funcs = new();
            List<string> func = new();
            string funcName = "";

            bool inFunc = false;

            c.vars = vars;
            foreach (string l in interLangLns)
            {
                bool i1 = inFunc;
                string[] i = l.Split(";");
                string[] ln = i[1].Split(",");
                c.stopIndex = int.Parse(i[0]);
                //Console.WriteLine(l);
                MTSError err;
                MTSConsole flexC, funcC;
                try
                {
                    if (!inFunc)
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
                                    "UnexpectedKeyword" => new MTSError.UnexpectedKeyword(),
                                    _ => new()
                                };
                                err.message += ln[4];
                                c.exitCode = err.ThrowErr(ln[2], int.Parse(ln[3]), ref c);
                                goto end;
                            case "SETVAR":
                                string[] eq = ln[1].Split("=");
                                vars[eq[0]] = eq[1];
                                break;

                            // the FLEX module: FiLe EXecutor
                            case "FLEX:EXECFL":
                                try
                                {
                                    flexC = runFromCode(File.ReadAllLines(ln[1]), ln[1]);
                                    c += flexC;
                                }
                                catch (FileNotFoundException)
                                {
                                    err = new MTSError.FileNotFound();
                                    err.message += ln[1];
                                    err.ThrowErr(fileName, c.stopIndex, ref c);
                                    goto end;
                                }
                                break;
                            case "FLEX:LOADVARS":
                                try
                                {
                                    flexC = runFromCode(File.ReadAllLines(ln[1]), ln[1]);
                                    c.copyVars(flexC);
                                }
                                catch (FileNotFoundException)
                                {
                                    err = new MTSError.FileNotFound();
                                    err.message += ln[1];
                                    err.ThrowErr(fileName, c.stopIndex, ref c);
                                    goto end;
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
                                    goto end;
                                }
                                else
                                {
                                    funcC = runFromInterLang(cd1, $"{fileName}:<function {ln[1]}>");
                                    c += funcC;
                                }
                                break;
                        }
                        c.vars = vars;
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
                }
                catch (Exception e)
                {
                    err = new MTSError.InternalError(e);
                    err.message += e.Message;
                    err.ThrowErr(fileName, c.stopIndex, ref c);
                    c.exitCode = err.code;
                    goto end;
                }
            }
            end: return c;
        }
        public static MTSConsole runFromInterLang(string interLangCode, string fileName)
            => runFromInterLang(interLangCode.ReplaceLineEndings("\n").Split("\n"), fileName);

        public static MTSConsole runFromCode(string[] lns, string fileName)
            => runFromInterLang(InterLang.toInterLang(lns, fileName), fileName);
        public static MTSConsole runFromCode(string code, string fileName)
            => runFromCode(code.ReplaceLineEndings("\n").Split("\n"), fileName);
    }
}