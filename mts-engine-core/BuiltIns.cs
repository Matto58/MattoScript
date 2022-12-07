namespace Mattodev.MattoScript.Engine
{
    public partial class CoreEng
    {
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