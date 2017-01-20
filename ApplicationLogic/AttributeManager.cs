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
            // If file or directory doesn't exist, don't change attributes
            if (isDirectory)
            {
                if (!Directory.Exists(source.FullName) || !Directory.Exists(target.FullName))
                {
                    return;
                }
            }
            else
            {
                if (!File.Exists(source.FullName) || !File.Exists(target.FullName))
                {
                    return;
                }
            }

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
                throw new NotImplementedException();
            }
            else
            {
                return true;
            }
        }
    }
}
