namespace Mattodev.MattoScript.Engine
{
    public partial class CoreEng
    {
        public class MTSFuncs
        {
            public class FiMa
            {
                public class Read : MTSFunc
                {
                    public Read() : base("fima.read") { }

                    public override void Exec(ref MTSConsole con, string[] args, string fileName, ref bool exit)
                    {
                        string[] a = args[1].Split('=');
                        try
                        {
                            con.vars[a[0]] = File.ReadAllText(a[1]);
                        }
                        catch (FileNotFoundException)
                        {
                            MTSError.FileNotFound err = new();
                            err.message += a[1];
                            err.ThrowErr(fileName, con.stopIndex, ref con);
                            con.exitCode = err.code;
                            exit = true;
                        }
                    }

                    public override string ToInterlang(int i, string[] args, string fileName)
                    {
                        try
                        {
                            return $"{i};{interName},{args[1]}";
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return $"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{args.Length}";
                        }
                    }
                }

                public class Write : MTSFunc
                {
                    public Write() : base("fima.write") { }

                    public override void Exec(ref MTSConsole con, string[] args, string fileName, ref bool exit)
                    {
                        File.WriteAllText(args[2], con.vars[args[1]]);
                    }

                    public override string ToInterlang(int i, string[] args, string fileName)
                    {
                        try
                        {
                            return $"{i};{interName},{args[1]},{args[2]}";
                        }
                        catch (IndexOutOfRangeException)
                        {
                            return $"{i};INTERNAL:ERR_THROW,TooLittleArgs,{fileName},{i},{args.Length}";
                        }
                    }
                }
            }
        }
    }
}
