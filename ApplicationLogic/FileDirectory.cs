using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    [Serializable()]
    public class FileDirectory
    {
        public string DirectoryPath { get; set; }

        public FileDirectory(string directory)
        {
            this.DirectoryPath = directory;
        }

        protected FileDirectory()
        {

        }

        public override string ToString()
        {
            return this.DirectoryPath;
        }
    }
}
