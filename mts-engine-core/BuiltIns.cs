namespace Mattodev.MattoScript.Engine
{
    public class CoreEng
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
            public static string engVer = "0.1.0.1";
            public static string mtsVer = "1";
        }
        public class MTSConsole
        {
            public string cont { get; set; }
            public string title { get; set; }
            public ConsoleColor fgColor { get; set; }
            public ConsoleColor bgColor { get; set; }
            public int exitCode { get; set; }
            public int stopIndex { get; set; }

            public MTSConsole()
            {
                cont = "";
                title = $"MattoScript v{MTSInfo.mtsVer} window (engine version {MTSInfo.engVer})";

                fgColor = ConsoleColor.Gray;
                bgColor = ConsoleColor.Black;
            }

            public void disp()
            {
                Console.WriteLine(cont);
                //Console.WriteLine(strjoin(cont, "\n"));
            }
        }
        public class MTSVar
        {
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

        public class BuiltIns
        {
            public class Console
            {
                public static void conOut(ref MTSConsole console, string txt = "", bool newLn = false)
                {
                    console.cont += txt;
                    if (newLn) console.cont += "\n";
                }
            }
        }
    }
}