using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class AttributeManager
    {
        public static void CopyAttributes(FileSystemInfo source, FileSystemInfo target, bool isDirectory)
        {
            target.Attributes = source.Attributes;

            // Weird behaviour on directories, other call fixes it.
            if (isDirectory)
            {
                Directory.SetCreationTime(target.FullName, source.CreationTime);
                Directory.SetCreationTimeUtc(target.FullName, source.CreationTimeUtc);
                Directory.SetLastWriteTime(target.FullName, source.LastWriteTime);
                Directory.SetLastWriteTimeUtc(target.FullName, source.LastWriteTimeUtc);
                Directory.SetLastAccessTime(target.FullName, source.LastAccessTime);
                Directory.SetLastAccessTimeUtc(target.FullName, source.LastAccessTimeUtc);
            }
            else
            {
                target.CreationTime = source.CreationTime;
                target.CreationTimeUtc = source.CreationTimeUtc;
                target.LastWriteTime = source.LastWriteTime;
                target.LastWriteTimeUtc = source.LastWriteTimeUtc;
                target.LastAccessTime = source.LastAccessTime;
                target.LastAccessTimeUtc = source.LastAccessTimeUtc;
            }

            SameAttributes(source, target);
        }

        public static bool SameAttributes(FileSystemInfo source, FileSystemInfo target)
        {
            if (target.Attributes != source.Attributes ||
                target.CreationTime != source.CreationTime ||
                target.CreationTimeUtc != source.CreationTimeUtc ||
                target.LastWriteTime != source.LastWriteTime ||
                target.LastWriteTimeUtc != source.LastWriteTimeUtc ||
                target.LastAccessTime != source.LastAccessTime ||
                target.LastAccessTimeUtc != source.LastAccessTimeUtc)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
