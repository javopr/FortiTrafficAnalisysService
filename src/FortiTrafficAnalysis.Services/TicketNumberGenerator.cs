using System;
using System.Linq;
using System.Text;

namespace FortiTrafficAnalysis.Services
{
    public interface ITicketNumberGenerator
    {
        string Generate();
    }

    public class TicketNumberGenerator : ITicketNumberGenerator
    {
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly Random Random = new Random();

        public string Generate()
        {
            var ticketNumber = new StringBuilder(10);
            
            for (int i = 0; i < 10; i++)
            {
                ticketNumber.Append(Characters[Random.Next(Characters.Length)]);
            }

            return ticketNumber.ToString();
        }
    }
}
