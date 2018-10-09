using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace KLHockeyBot.Network
{
    public class Ssl
    {
        public static bool Validator(object sender, X509Certificate certificate, X509Chain chain,
                              SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
