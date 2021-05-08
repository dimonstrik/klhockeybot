using System.Collections.Generic;
using System.Linq;

namespace KLHockeyBot.Entities
{
    public class HockeyPoll
    {
        public string Question { get; set; }
        public int MessageId { get; set; }
        public int Id { get; set; }
        public List<Vote> Votes { get; set; }

        public string Report
        {
            get
            {
                var yesCnt = Votes.Count(x => x.Data == "Да");
                var detailedResult = $"\nДа – {yesCnt}\n";
                var votes = Votes.FindAll(x => x.Data == "Да");
                foreach (var v in votes)
                {
                    var username = string.IsNullOrEmpty(v.Username) ? "" : $"(@{v.Username})";
                    detailedResult += $" {v.Name} {v.Surname} {username}\n";
                }
                if (votes.Count == 0) detailedResult += " -\n";

                var noCnt = Votes.Count(x => x.Data == "Не");
                detailedResult += $"\nНе – {noCnt}\n";
                votes = Votes.FindAll(x => x.Data == "Не");
                foreach (var v in Votes.FindAll(x => x.Data == "Не"))
                {
                    var username = string.IsNullOrEmpty(v.Username) ? "" : $"(@{v.Username})";
                    detailedResult += $" {v.Name} {v.Surname} {username}\n";
                }
                if (votes.Count == 0) detailedResult += " -\n";

                var cnt = yesCnt + noCnt;
                var answer = $"*{Question}*\n{detailedResult}\n👥 {cnt} человек проголосовало.";
                return answer.Replace("_", @"\_"); //Escaping underline in telegram api when parse_mode = Markdown
            }
        }
    }
}
