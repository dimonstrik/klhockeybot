using System;
using System.IO;
using System.Net;
using KLHockeyBot.Configs;
using KLHockeyBot.DB;
namespace KLHockeyBot
{
    class Program
    {
        private static bool InitFromCode = false;

        static void Main(string[] args)
        {
            //to ignore untrusted SSL certificates, linux and mono love it ;)
            ServicePointManager.ServerCertificateValidationCallback = Network.Ssl.Validator;

            Console.CancelKeyPress += Console_CancelKeyPress;

            if (!File.Exists(Config.DbFile) || InitFromCode || args.Length > 0 && args[0] == "init")
            {
                try
                {
                    DbCore.Initialization();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown DBCore exception: " + e.Message + "\n" + e.InnerException);
                }
            }

            Console.WriteLine("Starting Bot...");
            try
            {
                if (!Directory.Exists(Config.DbDirPath)) Directory.CreateDirectory(Config.DbDirPath);
                if (!Directory.Exists(Config.DbSourceDirPath)) Directory.CreateDirectory(Config.DbSourceDirPath);
                if (!Directory.Exists(Config.DbPlayersPhotoDirPath)) Directory.CreateDirectory(Config.DbPlayersPhotoDirPath);

                Bot.HockeyBot.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown KLHockey Bot exception: " + e.Message + "\n" + e.InnerException);
                Console.WriteLine("Bot will be terminated.");
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("It's the end! Bye.");
            Bot.HockeyBot.End = false;
            if (!Bot.HockeyBot.End) e.Cancel = true;
        }
    }
}
