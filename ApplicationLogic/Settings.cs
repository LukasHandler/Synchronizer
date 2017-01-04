using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    [Serializable()]
    public class Settings
    {
        public Settings()
        {
            this.BlockCompareMinFileSize = 1024;
            this.BlockCompareBlockSize = 100;
            this.ParallelSync = true;
        }

        /// <summary>
        /// Gets or sets the minimum size of a file when the block comparison gets used.
        /// </summary>
        /// <value>
        /// The minimum size of a file when the block comparison gets used.
        /// </value>
        public int BlockCompareMinFileSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the block compare block.
        /// </summary>
        /// <value>
        /// The size of the block compare block.
        /// </value>
        public int BlockCompareBlockSize { get; set; }

        public bool ParallelSync { get; set; }

        public override string ToString()
        {
            return string.Format("Minimum file size to use block comparison: " + this.BlockCompareMinFileSize +
                                "\r\nThe size of the blocks for block comparison: " + this.BlockCompareBlockSize +
                                "\r\nUse parallel synchronization: " + this.ParallelSync);
        }
    }
}
