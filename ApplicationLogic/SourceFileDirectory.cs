using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    [Serializable()]
    public class SourceFileDirectory : FileDirectory
    {
        public List<FileDirectory> TargetDirectories { get; set; }

        public List<FileDirectory> Exceptions { get; set; }

        public SourceFileDirectory(string directoryPath) : base(directoryPath)
        {
            this.TargetDirectories = new List<FileDirectory>();
        }

        private SourceFileDirectory()
        {

        }

        public override string ToString()
        {
            return this.DirectoryPath;
        }
    }
}
