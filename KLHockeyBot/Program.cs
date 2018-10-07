using System;
using System.Net;

namespace KLHockeyBot
{
    class Program
    {
        private static bool InitFromCode = true;

        static void Main(string[] args)
        {
            //to ignore untrusted SSL certificates, linux and mono love it ;)
            ServicePointManager.ServerCertificateValidationCallback = Network.SSL.Validator;

            Console.CancelKeyPress += Console_CancelKeyPress;
            DBCore.InitializationOnlyEvents();

            if (InitFromCode || args.Length > 0 && args[0] == "init")
            {
                try
                {
                    DBCore.Initialization();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown DBCore exception: " + e.Message + "\n" + e.InnerException);
                }
            }

            Console.WriteLine("Starting Bot...");
            try
            {
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
