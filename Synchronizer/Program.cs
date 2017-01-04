using Synchronizer.ApplicationLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.PresentationLogic
{
    class Program
    {
        static void Main(string[] args)
        {
            PresentationManager presentationManager = new PresentationManager();
            presentationManager.Start();
        }
    }
}
