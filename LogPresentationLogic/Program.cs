//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file represents the logging window.
// </summary>
//-----------------------------------------------------------------------
namespace LogPresentationLogic
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// This class contains the logging window.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Starts an UDP client to receive logging messages.
        /// </summary>
        /// <param name="args">The arguments, not used.</param>
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
