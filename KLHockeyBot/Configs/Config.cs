using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System;

namespace KLHockeyBot.Configs
{
    public static class Config
    {
        public static readonly string BotToken = ConfigurationManager.AppSettings["BotToken"];

        public static readonly string DBPlayersPhotoDirPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBPlayersPhotoDirPath"];
        public static readonly string DBPlayersInfoPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBPlayersInfoPath"];
        public static readonly string DBEventsInfoPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBEventsInfoPath"];        
        public static readonly string DBGamesInfoPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBGamesInfoPath"];
        public static readonly string DBFile = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBFile"];

        public static readonly string SportFortTeamMembersPage = ConfigurationManager.AppSettings["TeamMembersPage"];
        public static readonly string PWD = ConfigurationManager.AppSettings["PWD"];


        public static class BotAdmin
        {
            private static readonly List<int> Admins = new List<int>();
            
            public static bool isAdmin(int id)
            {
                return Admins.Contains(id);
            }

            static BotAdmin()
            {
                var keys = ConfigurationManager.AppSettings.AllKeys;
                foreach (var key in keys)
                {
                    try
                    {
                        if (key.Contains("Admin")) Admins.Add(int.Parse(ConfigurationManager.AppSettings[key]));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("BotAdmin initialization error: " + ex.Message);
                    }
                }
            }
        }
    }
}
