//-----------------------------------------------------------------------
// <copyright file="LoggingManager.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file represents the logging manager.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// This class represents the manager responsible for logging.
    /// </summary>
    public static class LoggingManager
    {
        /// <summary>
        /// A locker for thread save log message write operations.
        /// </summary>
        private static object locker;

        /// <summary>
        /// Initializes static members of the <see cref="LoggingManager"/> class.
        /// </summary>
        static LoggingManager()
        {
            locker = new object();
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Log(string message)
        {
            string log = DateTime.Now.ToString("dd.MM.yyyy-HH:mm:ss:FF") + " " + message;
            SendLogMessage(log);
            WriteLogFile(log);
        }

        /// <summary>
        /// Writes the log message in the log file.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
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
            }
        }

        /// <summary>
        /// Sends the log message to the logging project.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
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
