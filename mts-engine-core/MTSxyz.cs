namespace Mattodev.MattoScript.Engine
{
    public partial class CoreEng
    {
        public static string strjoin(string[] strArr, string sep = ",")
        {
            string o = "";
            foreach (string i in strArr) o += i + sep;
            try
            {
                return o[..^sep.Length];
            }
            catch
            {
                return o;
            }
        }
        public class MTSInfo
        {
            public static string engVer = "0.2.0.7";
            public static string mtsVer = "1";
        }
        public class MTSConsole
        {
            public string cont { get; set; }
            public string title { get; set; }
            public int exitCode { get; set; }
            public int stopIndex { get; set; }
            public Dictionary<string, string> vars { get; set; }
            public Dictionary<string, Int128> intVars { get; set; }
            public Dictionary<string, string[]> funcs { get; set; }

            public MTSConsole()
            {
                cont = "";
                title = $"MattoScript v{MTSInfo.mtsVer} window (engine version {MTSInfo.engVer})";
                vars = new();
                intVars = new();
                funcs = new();
            }

            public override string ToString() => cont;

            public void disp() => Console.Write(cont);
            public void clr() => cont = "";

            public void copyVars(MTSConsole fromConsole)
            {
                foreach (var v in fromConsole.vars)
                    vars[v.Key] = v.Value;

                foreach (var v in fromConsole.intVars)
                    intVars[v.Key] = v.Value;
            }
            public void copyFuncs(MTSConsole fromConsole)
            {
                foreach (var f in fromConsole.funcs)
                    if (!funcs.ContainsKey(f.Key))
                        funcs[f.Key] = f.Value;
            }

            public static MTSConsole operator +(MTSConsole a, MTSConsole b)
            {
                MTSConsole c = a;
                c.cont = a.cont + b.cont;
                c.copyVars(b);
                c.copyFuncs(b);
                return c;
            }
            public static MTSConsole operator *(MTSConsole a, int b)
            {
                MTSConsole c = a;
                if (b < 0) throw new IndexOutOfRangeException();
                if (b == 0)
                {
                    c.cont = "";
                    return c;
                }
                for (int i = 1; i < b; i++) c.cont += a.cont;
                return c;
            }
            public static explicit operator string(MTSConsole c) => c.ToString();
        }
        public class MTSVar
        {
            // this class is unused, keeping for backwards compatibility
            public string varName { get; set; }
            public string varValue { get; set; }

            public MTSVar(string name, string val)
            {
                varName = name;
                varValue = val;
            }

            public static string search(List<MTSVar> vars, string varName)
            {
                foreach (MTSVar v in vars) if (v.varName == varName) return v.varValue;
                return "";
            }
        }
        public abstract class MTSFunc
        {
            public string interName = "";
            public abstract string ToInterlang(int i, string[] args, string fileName);
            public abstract void Exec(ref MTSConsole con, string[] args, string fileName, ref bool exit);

            public MTSFunc(string funcName)
            {
                interName = GenInterName(funcName);
            }

            // recommended to use to generate an interlang name (a name used in the interlang)
            public static string GenInterName(string funcName)
                => funcName.Replace(".", ":").ToUpper();

            public static MTSError GetErrFromName(string name)
                => name switch
                {
                    "InvalidCommand" => new MTSError.InvalidCommand(),
                    "TooLittleArgs" => new MTSError.TooLittleArgs(),
                    "FileNotFound" => new MTSError.FileNotFound(),
                    "UnassignedVar" => new MTSError.UnassignedVar(),
                    "NoVarVal" => new MTSError.NoVarVal(),
                    "UnexpectedKeyword" => new MTSError.UnexpectedKeyword(),
                    "InvalidInt" => new MTSError.InvalidInt(),
                    "InvalidArg" => new MTSError.InvalidArg(),
                    _ => new()
                };
        };
    }
}
