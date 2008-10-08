using System;
using System.Collections.Generic;
using System.IO;
using CommandLine.Utility;

namespace OpenMetaverse.TestClient
{
    public class CommandLineArgumentsException : Exception
    {
    }

    public class Program
    {
        private static void Usage()
        {
            Console.WriteLine("Usage: " + Environment.NewLine +
                    "TestClient.exe --first firstname --last lastname --pass password --contact \"youremail\" [--startpos \"sim/x/y/z\"] [--master \"master name\"] [--masterkey \"master uuid\"] --loginuri=\"uri\"");
        }

        static void Main(string[] args)
        {
            Arguments arguments = new Arguments(args);

            ClientManager manager;
            List<LoginDetails> accounts = new List<LoginDetails>();
            LoginDetails account;
            bool groupCommands = false;
            string masterName = String.Empty;
            UUID masterKey = UUID.Zero;
            string file = String.Empty;
            string loginuri = String.Empty;

            if (arguments["groupcommands"] != null)
                groupCommands = true;

            if (arguments["masterkey"] != null)
                masterKey = UUID.Parse(arguments["masterkey"]);

            if (arguments["master"] != null)
                masterName = arguments["master"];

            if (arguments["loginuri"] != null)
                loginuri = arguments["loginuri"];

            if (arguments["file"] != null)
            {
                file = arguments["file"];

                if (!File.Exists(file))
                {
                    Console.WriteLine("File {0} Does not exist", file);
                    return;
                }

                // Loading names from a file
                try
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string line;
                        int lineNumber = 0;

                        while ((line = reader.ReadLine()) != null)
                        {
                            lineNumber++;
                            string[] tokens = line.Trim().Split(new char[] { ' ', ',' });

                            if (tokens.Length >= 3)
                            {
                                account = new LoginDetails();
                                account.FirstName = tokens[0];
                                account.LastName = tokens[1];
                                account.Password = tokens[2];

                                accounts.Add(account);
                            }
                            else
                            {
                                Logger.Log("Invalid data on line " + lineNumber +
                                    ", must be in the format of: FirstName LastName Password",
                                    Helpers.LogLevel.Warning);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading from " + args[1]);
                    Console.WriteLine(e.ToString());
                    return;
                }
            }
            else if (arguments["first"] != null && arguments["last"] != null && arguments["pass"] != null)
            {
                // Taking a single login off the command-line
                account = new LoginDetails();
                account.FirstName = arguments["first"];
                account.LastName = arguments["last"];
                account.Password = arguments["pass"];

                accounts.Add(account);
            }
            else if (arguments["help"] != null)
            {
                Usage();
                return;
            }

            foreach (LoginDetails a in accounts)
            {
                a.GroupCommands = groupCommands;
                a.MasterName = masterName;
                a.MasterKey = masterKey;
                a.URI = loginuri;
            }

            // Login the accounts and run the input loop
            if (arguments["startpos"] != null)
                manager = new ClientManager(accounts, arguments["startpos"]);
            else
                manager = new ClientManager(accounts);

            manager.Run();
        }
    }
}
