using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LogPresentationLogic
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.SetWindowSize(Console.WindowWidth + 50, Console.WindowHeight);
                UdpClient client = new UdpClient(12223);
                while (true)
                {
                    IPEndPoint server = null;
                    byte[] logMessage = client.Receive(ref server);
                    Console.WriteLine(Encoding.ASCII.GetString(logMessage));
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            }
        }
    }
}
