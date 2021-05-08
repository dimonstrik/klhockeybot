using System.Collections.Generic;
using System.Linq;

namespace KLHockeyBot.Entities
{
    public class HockeyPayment
    {
        public string Name { get; set; }
        public int MessageId { get; set; }
        public int Id { get; set; }
        public List<Payer> Payers { get; set; }

        public string Report
        {
            get
            {
                var count = Payers.Count();
                var totalAmount = Payers.Sum(x => x.Amount)/100;
                var detailedResult = "";
                if (count == 0) detailedResult += " -\n";
                else
                    foreach (var p in Payers)
                    {
                        var username = string.IsNullOrEmpty(p.Username) ? "" : $"(@{p.Username})";
                        detailedResult += $" {p.Name} {p.Surname} {username}\n";
                    }

                var answer = $"*{Name}*\n\n{detailedResult}\n👥 {count} оплатили на сумму {totalAmount}RUB.";
                return answer.Replace("_", @"\_"); //Escaping underline in telegram api when parse_mode = Markdown
            }
        }
    }
}
