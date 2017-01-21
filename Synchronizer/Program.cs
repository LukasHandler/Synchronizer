using Synchronizer.ApplicationLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.PresentationLogic
{
    class Program
    {
        static void Main(string[] args)
        {
            Process loggingProcess = null;

            try
            {
                loggingProcess = Process.Start("LogPresentationLogic.exe");
            }
            catch(Exception exception)
            {
                Console.WriteLine("Logging window couldn't start\r\n" + exception.Message);
            }

            PresentationManager presentationManager = new PresentationManager(loggingProcess);
            presentationManager.Start();
        }
    }
}
