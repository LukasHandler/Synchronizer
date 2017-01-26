//-----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains the settings of the program.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.IO;

    /// <summary>
    /// This class contains the settings of the program.
    /// </summary>
    [Serializable]
    public class Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        public Settings()
        {
            this.BlockCompareMinFileSize = 1024;
            this.BlockCompareBlockSize = 100;
            this.ParallelSync = true;
            this.LoggingFile = new FileInfo("Logs.txt");
            this.MaxLoggingFileSize = 1024;
        }

        /// <summary>
        /// Gets or sets the minimum size of a file when the block comparison gets used.
        /// </summary>
        /// <value>
        /// The minimum size of a file when the block comparison gets used.
        /// </value>
        public long BlockCompareMinFileSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the block compare block.
        /// </summary>
        /// <value>
        /// The size of the block compare block.
        /// </value>
        public int BlockCompareBlockSize { get; set; }

        /// <summary>
        /// Gets or sets the logging file.
        /// </summary>
        /// <value>
        /// The logging file.
        /// </value>
        public FileInfo LoggingFile { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the logging file.
        /// </summary>
        /// <value>
        /// The maximum size of the logging file.
        /// </value>
        public long MaxLoggingFileSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it can parallel synchronize or not.
        /// </summary>
        /// <value>
        ///   True if it can parallel synchronize.
        /// </value>
        public bool ParallelSync { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Minimum file size to use block comparison: " + this.BlockCompareMinFileSize +
                                "\r\nThe size of the blocks for block comparison: " + this.BlockCompareBlockSize +
                                "\r\nUse parallel synchronization: " + this.ParallelSync);
        }
    }
}
