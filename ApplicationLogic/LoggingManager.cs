using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class LoggingManager
    {
        private static object locker;

        static LoggingManager()
        {
            locker = new object();
        }

        public static void Log(string message)
        {
            string log = DateTime.Now.ToString("dd.MM.yyyy-HH:mm:ss:FF") + " " + message;
            SendLogMessage(log);
            WriteLogFile(log);
        }

        private static void WriteLogFile(string logMessage)
        {
            try
            {
                var loggingFile = ApplicationManager.Settings.LoggingFile;

                lock (locker)
                {
                    if (!loggingFile.Exists)
                    {
                        using (var stream = File.Create(loggingFile.FullName))
                        {
                        }
                    }

                    File.AppendAllText(loggingFile.FullName, logMessage + Environment.NewLine);
                    long maxFileSize = ApplicationManager.Settings.MaxLoggingFileSize;
                    long fileSize = new FileInfo(loggingFile.FullName).Length;

                    if (fileSize > maxFileSize)
                    {
                        // Create new logging file
                        var newLoggingFile = new FileInfo(loggingFile.FullName + ".bak");

                        // If exists already, delete it
                        if (newLoggingFile.Exists)
                        {
                            File.Delete(newLoggingFile.FullName);
                        }

                        File.Move(loggingFile.FullName, newLoggingFile.FullName);
                        using (var stream = File.Create(loggingFile.FullName))
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private static void SendLogMessage(string logMessage)
        {
            try
            {
                UdpClient client = new UdpClient();
                byte[] bytes = Encoding.ASCII.GetBytes(logMessage);
                client.Send(bytes, bytes.Length, new System.Net.IPEndPoint(IPAddress.Parse("127.0.0.1"), 12223));
            }
            catch (Exception)
            {

            }
        }
    }
}
