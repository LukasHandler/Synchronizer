using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class BlockComparer
    {
        public static void BlockCompare(string sourceFilePath, string targetFilePath)
        {
            // Both Files the same?
            if (!PathHelper.IsSamePath(sourceFilePath, targetFilePath))
            {
                FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
                FileStream targetStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.ReadWrite);

                long currentIndex = 0;
                int blockCompareSize = ApplicationManager.Settings.BlockCompareBlockSize;
                long sourceLength = sourceStream.Length;
                bool finished = false;

                targetStream.SetLength(sourceLength);

                do
                {
                    long leftSize = Convert.ToInt32(sourceLength) - currentIndex - blockCompareSize;


                    if (leftSize < 0)
                    {
                        blockCompareSize = Math.Abs(Convert.ToInt32(leftSize));
                        finished = true;
                    }

                    byte[] sourceBytes = new byte[blockCompareSize];
                    sourceStream.Read(sourceBytes, 0, sourceBytes.Length);

                    byte[] targetBytes = new byte[blockCompareSize];
                    targetStream.Read(targetBytes, 0, targetBytes.Length);

                    if (sourceBytes.Length != targetBytes.Length || sourceBytes.SequenceEqual(targetBytes))
                    {
                        targetStream.Write(targetBytes, 0, targetBytes.Length);
                    }

                    currentIndex += blockCompareSize;
                }
                while (!finished);

                sourceStream.Dispose();
                targetStream.Dispose();
            }
        }

    }
}
