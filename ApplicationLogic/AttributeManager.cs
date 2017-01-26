//-----------------------------------------------------------------------
// <copyright file="AttributeManager.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains the copy attributes mechanism.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.IO;

    /// <summary>
    /// This class contains the copy attributes mechanism.
    /// </summary>
    internal static class AttributeManager
    {
        /// <summary>
        /// Copies the attributes.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="isDirectory">If set to <c>true</c> it is a directory.</param>
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
        }
    }
}
