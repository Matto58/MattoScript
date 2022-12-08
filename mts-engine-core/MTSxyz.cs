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
            public static string engVer = "0.1.1.4";
            public static string mtsVer = "1";
        }
        public class MTSConsole
        {
            public string cont { get; set; }
            public string title { get; set; }
            public int exitCode { get; set; }
            public int stopIndex { get; set; }
            public Dictionary<string, string> vars { get; set; }

            public MTSConsole()
            {
                cont = "";
                title = $"MattoScript v{MTSInfo.mtsVer} window (engine version {MTSInfo.engVer})";
                vars = new();
            }

            public void disp() => Console.Write(cont);

            public void clr() => cont = "";

            public static MTSConsole operator +(MTSConsole a, MTSConsole b)
            {
                MTSConsole c = a;
                c.cont = a.cont + b.cont;
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
            public void copyVars(MTSConsole otherConsole)
            {
                foreach (var v in otherConsole.vars)
                    if (!vars.ContainsKey(v.Value))
                        vars[v.Key] = v.Value;
            }
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
    }
}
