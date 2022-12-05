using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Engine
{
    public class MTSError
    {
        public int code { get; set; }
        public string message { get; set; }

        public MTSError()
        {
            code = 0;
            message = "";
        }

        public int ThrowErr(string fileName, int line, ref MTSConsole con)
        {
            con.cont += (
                $"Error! MTS-{Convert.ToString(code, 16)}: {(code != -1 ? $"'{message}'" : message)}\n" +
                $"\tLine {line+1} in file {fileName}" 
            );
            return code;
        }

        public class InvalidCommand : MTSError
        {
            public InvalidCommand()
            {
                code = 1;
                message = "Invalid command: ";
            }
        }
        public class TooLittleArgs : MTSError
        {
            public TooLittleArgs()
            {
                code = 2;
                message = "Too little args; found ";
            }
        }
        public class FileNotFound : MTSError
        {
            public FileNotFound()
            {
                code = 3;
                message = "File not found: ";
            }
        }
        public class UnassignedVar : MTSError
        {
            public UnassignedVar()
            {
                code = 4;
                message = "Tried to use the unassigned variable ";
            }
        }
        public class NoVarVal : MTSError
        {
            public NoVarVal()
            {
                code = 5;
                message = "Variable cannot be empty: ";
            }
        }


        public class InternalError : MTSError
        {
            public InternalError()
            {
                code = -1;
                message = "Internal error: ";
            }
        }
    }
}
