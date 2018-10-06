using KLHockeyBot.Configs;
using System;
using System.Collections.Generic;
using System.IO;

namespace KLHockeyBot
{
    public class Randomiser
    {
        private readonly List<string> playersDescr = new List<string>();
        private readonly List<string> slogans = new List<string>();
        private readonly Random random = new Random();

        public Randomiser()
        {
            InitializateDescr();
            InitializateSlogans();
        }

        public string GetPlayerDescr()
        {
            var index = random.Next(playersDescr.Count);
            return playersDescr[index];
        }

        public string GetSlogan()
        {
            var index = random.Next(slogans.Count);
            return slogans[index];
        }

        private void InitializateDescr()
        {
            var players = File.ReadAllLines(Config.Descr);
            foreach (var player in players)
            {
                var playerinfo = player.Replace(';','\n');
                playersDescr.Add(playerinfo);
            }
        }

        private void InitializateSlogans()
        {
            var sls = File.ReadAllLines(Config.Slogans);
            foreach (var sl in sls)
            {
                var sloginfo = sl.Replace(';','\n');
                slogans.Add(sloginfo);
            }
        }
    }
}
