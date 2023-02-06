using System.Diagnostics;
using static Mattodev.MattoScript.Engine.CoreEng;

namespace Mattodev.MattoScript.Engine
{
	public class MTSError
	{
		public int code { get; set; }
		public string message { get; set; }
		public string id { get; set; }
		public Exception? cause { get; set; }

		public MTSError()
		{
			code = 0;
			message = "";
			id = "MTSError";
		}

		public int ThrowErr(string fileName, int line, ref MTSConsole con)
		{
			con.cont += (
				$"Error! ({id}) MTS-{Convert.ToString(code, 16)}: {(code != -1 ? $"'{message}'" : message)}\n" +
				$"\tLine {line + 1} in file {fileName}"
			);
			if (code == -1 && cause != null)
			{
				con.cont += "\n\tStack trace (report at github.com/Matto58/MattoScript):\n" + cause.StackTrace;
			}
			return code;
		}

		public override string ToString()
			=> id;

		public class InvalidCommand : MTSError
		{
			public InvalidCommand()
			{
				code = 1;
				message = "Invalid command: ";
				id = "InvalidCommand";
			}
		}
		public class TooLittleArgs : MTSError
		{
			public TooLittleArgs()
			{
				code = 2;
				message = "Too little args; found ";
				id = "TooLittleArgs";
			}
		}
		public class FileNotFound : MTSError
		{
			public FileNotFound()
			{
				code = 3;
				message = "File not found: ";
				id = "FileNotFound";
			}
		}
		public class UnassignedVar : MTSError
		{
			public UnassignedVar()
			{
				code = 4;
				message = "Tried to use the unassigned variable ";
				id = "UnassignedVar";
			}
		}
		public class NoVarVal : MTSError
		{
			public NoVarVal()
			{
				code = 5;
				message = "Variable cannot be empty: ";
				id = "NoVarVal";
			}
		}
		public class UnexpectedKeyword : MTSError
		{
			public UnexpectedKeyword()
			{
				code = 6;
				message = "Unexpected keyword: ";
				id = "UnexpectedKeyword";
			}
		}
		public class InvalidInt : MTSError
		{
			public InvalidInt()
			{
				code = 7;
				message = "This is not a valid 128-bit signed integer: ";
				id = "InvalidInt";
			}
		}
		public class InvalidArg : MTSError
		{
			public InvalidArg()
			{
				code = 8;
				message = "This is not a valid argument: ";
				id = "InvalidArg";
			}
		}
		public class TooLongExecution : MTSError
		{
			public TooLongExecution()
			{
				code = 9;
				message = "Loop was executed for too long.";
				id = "TooLongExecution";
			}
		}
		public class VarIsConst : MTSError
		{
			public VarIsConst()
			{
				code = 10;
				message = "Tried to modify constant variable: ";
				id = "VarIsConst";
			}
		}

		public class InternalError : MTSError
		{
			public InternalError(Exception e)
			{
				code = -1;
				message = "Internal error:\n\t";
				id = e.GetType().ToString();
				cause = e;
			}
		}
	}
}
