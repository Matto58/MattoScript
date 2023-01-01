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

        public static MTSConsole runFromInterLang(string[] interLangLns, string fileName, Dictionary<string, string> variables, Dictionary<string, Int128> intVariables)
        {
            MTSConsole c = new(), varC = new();
            Dictionary<string, string> vars = new()
            {
                { "$ver.engine", MTSInfo.engVer },
                { "$ver.mtscript", MTSInfo.mtsVer },
                { "$con.title", c.title }
            };
            Dictionary<string, Int128> intVars = new();
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

                if (exit) goto end;

                try
                {
                    if (!inFunc)
                    {
                        // might work? (ev0.2.0.6)
                        foreach (var f in otherFuncs)
                            if (f.Value.interName == ln[0])
                                f.Value.Exec(ref c, ln, fileName, ref exit);

                        foreach (var f in mainFuncs)
                            if (f.Value.interName == ln[0])
                                f.Value.Exec(ref c, ln, fileName, ref exit);

                        switch (ln[0])
                        {
                            case "CON:OUT":
                                //Console.WriteLine("\t" + intVars[ln[2]]);
                                string vVal = vars.GetValueOrDefault(ln[2], "INTERNAL:NOVAL");
                                Int128 vVal2 = intVars.GetValueOrDefault(ln[2], 0);
                                if (
                                    ((vVal == "INTERNAL:NOVAL" || vVal == "") && ln[2][0] == '$')
                                    || (vVal2 == Int128.Zero && ln[2][0] == '%')
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
                                    ln[2], ln[1] == "1"
                                );
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
                                err = MTSFunc.GetErrFromName(ln[1]);
                                err.message += ln[4];
                                c.exitCode = err.ThrowErr(ln[2], int.Parse(ln[3]), ref c);
                                exit = true;
                                break;
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
                                    exit = true;
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
                                    exit = true;
                                }
                                break;
                            case "FLEX:LOADFUNCS":
                                try
                                {
                                    flexC = runFromCode(File.ReadAllLines(ln[1]), ln[1]);
                                    c.copyFuncs(flexC);
                                }
                                catch (FileNotFoundException)
                                {
                                    err = new MTSError.FileNotFound();
                                    err.message += ln[1];
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
                                intVars[eqv[0]] = Int128.Parse(eqv[1]);
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