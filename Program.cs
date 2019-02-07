using System;
using System.IO;

namespace aflang
{
    class Program
    {
        static void Main(string[] args)
        {
            const string version = "0.3";
			Scanner sc;
			Parser par;
			string command = "";

			Console.Write("DustCat asm " + version + Environment.NewLine);
			while (command.ToUpper () != "QUIT") 
			{
				Console.Write (">");
				command = Console.ReadLine();
				string[] cmdSplit = command.Split(' ');

			    if (cmdSplit[0].ToUpper() != "DO") continue;
			    if (File.Exists(cmdSplit[1]))
			    {
			        sc = new Scanner(cmdSplit[1]);
			        par = new Parser(sc);
					par.tab = new SymbolTable(par);
					par.gen = new CodeGenerator();
					par.Parse();
					if (par.errors.count == 0)
					{
						//interpret and/or compile
						par.gen.Decode();
					}
			    }
			    else
			        Console.WriteLine("File does not exists !");
			}
        }
    }
}
