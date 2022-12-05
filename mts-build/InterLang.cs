using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Builder
{
    public class InterLang
    {
        public static string[] toInterLang(string[] lns, string fileName)
        {
            List<string> oc = new();
            Dictionary<string, string> vars = new();
            for (int i = 0; i < lns.Length; i++)
            {
                string l = lns[i].Split("#")[0];
                if (!string.IsNullOrWhiteSpace(l))
                {
                    string[] ln = l.Split(" ");
                    //Console.WriteLine("'" + ln[0] + "'");

                    switch (ln[0])
                    {
                        case "con.out":
                            try
                            {
                                oc.Add($"{i};CON:OUT,{(ln[1].ToLower() == "nl" ? "1" : "0")},{strjoin(ln[2..], " ")}");
                            }
                            catch (IndexOutOfRangeException)
                            {
                                oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
                                goto end;
                            }
                            break;
                        // update: variable assignment fixed in engine 0.1.0.2
                        case "var":
                            string[] v;
                            try
                            {
                                v = strjoin(ln[1..], " ").Split("=");
                            }
                            catch (IndexOutOfRangeException)
                            {
                                oc.Add($"{i};INTERNAL:ERR_THROW,NoVarVal,{fileName},{i},{ln[1]}");
                                goto end;
                            }
                            try
                            {
                                oc.Add($"{i};SETVAR,{v[0]}={v[1]}");
                            }
                            catch (IndexOutOfRangeException)
                            {
                                oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length},{ln[0]}");
                                goto end;
                            }
                            break;
                        case "halt": goto end;
                        case "throw":
                            try
                            {
                                oc.Add($"{i};INTERNAL:ERR_THROW,{strjoin(ln[1..])}");
                            }
                            catch (IndexOutOfRangeException)
                            {
                                oc.Add($"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{ln.Length}");
                                goto end;
                            }
                            break;
                        default:
                            oc.Add($"{i};INTERNAL:ERR_THROW,InvalidCommand,{fileName},{i},{ln[0]}");
                            goto end;
                    }
                }
            }
            //Console.WriteLine(oc.Count);
            end: return oc.ToArray();
        }
        public static string[] toInterLang(string code, string fileName)
            => toInterLang(code.ReplaceLineEndings("\n").Split("\n"), fileName);
    }
}