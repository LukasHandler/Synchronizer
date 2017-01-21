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
        public string Path { get; set; }

        public FileDirectory(string path)
        {
            this.Path = path;
        }

        public override string ToString()
        {
            return this.Path;
        }
    }
}
