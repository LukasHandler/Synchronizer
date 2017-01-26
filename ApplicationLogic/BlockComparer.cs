//-----------------------------------------------------------------------
// <copyright file="BlockComparer.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains the block comparison.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// This class contains the block comparison.
    /// </summary>
    public static class BlockComparer
    {
        /// <summary>
        /// The locker for thread save block comparison.
        /// </summary>
        private static object locker = new object();

        /// <summary>
        /// Compares the blocks of two files and makes them the same.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="targetFilePath">The target file path.</param>
        public static void BlockCompare(string sourceFilePath, string targetFilePath)
        {
            lock (locker)
            {
                // Both Files the same?
                FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
                FileStream targetStream = new FileStream(targetFilePath, FileMode.Open, FileAccess.ReadWrite);

                sourceStream.Position = 0;
                targetStream.Position = 0;

                long currentIndex = 0;
                int blockCompareSize = ApplicationManager.Settings.BlockCompareBlockSize;
                long sourceLength = sourceStream.Length;
                bool finished = false;

                targetStream.SetLength(sourceLength);

                do
                {
                    long leftSize = Convert.ToInt32(sourceLength) - currentIndex;

                    if (leftSize < blockCompareSize)
                    {
                        if (leftSize != 0)
                        {
                            blockCompareSize = Math.Abs(Convert.ToInt32(leftSize));
                        }

                        finished = true;
                    }

                    byte[] sourceBytes = new byte[blockCompareSize];
                    sourceStream.Read(sourceBytes, 0, sourceBytes.Length);

                    byte[] targetBytes = new byte[blockCompareSize];
                    targetStream.Read(targetBytes, 0, targetBytes.Length);

                    if (sourceBytes.Length != targetBytes.Length || !sourceBytes.SequenceEqual(targetBytes))
                    {
                        // Stelle überschreiben
                        targetStream.Position = currentIndex;
                        targetStream.Write(sourceBytes, 0, sourceBytes.Length);
                    }

                    currentIndex += blockCompareSize;
                }
                while (!finished);

                targetStream.SetLength(sourceLength);

                sourceStream.Position = 0;
                targetStream.Position = 0;
                sourceStream.Dispose();
                targetStream.Dispose();
            }
        }
    }
}
