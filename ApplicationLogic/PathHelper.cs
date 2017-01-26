//-----------------------------------------------------------------------
// <copyright file="PathHelper.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file contains path helping methods.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// This class contains path helping methods.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Determines whether two file system info are the same path.
        /// </summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns>True if they have the same path.</returns>
        public static bool IsSamePath(FileSystemInfo path1, FileSystemInfo path2)
        {
            if (path1.FullName.ToLower().TrimEnd('\\') == path2.FullName.ToLower().TrimEnd('\\'))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the path to default path.
        /// </summary>
        /// <param name="path">The path which should be changed.</param>
        /// <param name="isFile">If set to <c>true</c> it is a file.</param>
        /// <returns>The new file path with default path layout.</returns>
        public static string ChangePathToDefaultPath(string path, bool isFile = false)
        {
            string newPath = path.Replace('/', '\\').Trim().TrimEnd('\\').ToLower();

            if (isFile)
            {
                var objectName = newPath.Split('\\').Last();
                int index = newPath.IndexOf(objectName);
                string dictionaryPath = PathHelper.ChangePathToDefaultPath(newPath.Remove(index, objectName.Length));
                if (dictionaryPath != "\\")
                {
                    newPath = dictionaryPath.ToLower() + objectName;
                }
            }
            else
            {
                newPath += '\\';
            }

            return newPath;
        }

        /// <summary>
        /// Gets the name of the logical drive.
        /// </summary>
        /// <param name="path">The path including the logical drive.</param>
        /// <returns>The logical drive.</returns>
        public static string GetLogicalDriveName(string path)
        {
            string defaultPath = ChangePathToDefaultPath(path);

            if (defaultPath.Contains(':'))
            {
                return defaultPath.Split(':').First();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether this instance can synchronize the specified targets.
        /// </summary>
        /// <param name="targets">The targets.</param>
        /// <returns>True if it can synchronize.</returns>
        public static bool CanSynchronize(List<DirectoryInfo> targets)
        {
            List<string> pathes = targets.Select(p => p.FullName).ToList();

            if (pathes == null || pathes.Count <= 1)
            {
                return false;
            }

            List<string> foundLogicalNames = new List<string>();

            foreach (var path in pathes)
            {
                string logicalName = string.Empty;

                if (path.StartsWith("\\\\"))
                {
                    return false;
                }
                else
                {
                    logicalName = GetLogicalDriveName(path);
                }

                if (string.IsNullOrEmpty(logicalName))
                {
                    return false;
                }

                if (foundLogicalNames.Contains(logicalName))
                {
                    return false;
                }
                else
                {
                    foundLogicalNames.Add(logicalName);
                }
            }

            return true;
        }

        /// <summary>
        /// Validates the whole structure.
        /// </summary>
        /// <param name="sourceDirectories">The source directories to validate.</param>
        /// <returns>A possible error message if the structure is invalid.</returns>
        public static string IsValid(List<SourceFileDirectory> sourceDirectories)
        {
            // Validate all pathes and change the layout.
            for (int sourceIndex = 0; sourceIndex < sourceDirectories.Count; sourceIndex++)
            {
                var source = sourceDirectories[sourceIndex];

                // Check source.
                if (!source.DirectoryPath.Exists)
                {
                    return string.Format("Source {0} {1} doesn't exist", sourceIndex, source.DirectoryPath);
                }
                else
                {
                    source.DirectoryPath = new DirectoryInfo(ChangePathToDefaultPath(source.DirectoryPath.FullName));
                }

                // Check all targets.
                for (int i = 0; i < source.Targets.Count; i++)
                {
                    var target = source.Targets[i];

                    if (!target.Exists)
                    {
                        return string.Format("Target {0} {1} doesn't exist", i, target.FullName);
                    }
                    else
                    {
                        target = new DirectoryInfo(ChangePathToDefaultPath(target.FullName));
                    }
                }

                // Check all exceptions.
                for (int j = 0; j < source.Exceptions.Count; j++)
                {
                    var exception = source.Exceptions[j];
                    if (!exception.Exists)
                    {
                        return string.Format("Exception {0} {1} doesn't exist", j, exception.FullName);
                    }
                    else
                    {
                        exception = new DirectoryInfo(ChangePathToDefaultPath(exception.FullName));
                    }
                }
            }

            // Validate each source
            foreach (var source in sourceDirectories)
            {
                int sourceIndex = sourceDirectories.FindIndex(p => p == source);

                // Aagainst the other sources.
                // The same source twice?
                if (sourceDirectories.Where(p => IsSamePath(p.DirectoryPath, source.DirectoryPath)).Count() > 1)
                {
                    return string.Format("The source {0} appears more than once in sources", source.DirectoryPath);
                }

                // The same target twice
                foreach (var target in source.Targets)
                {
                    if (source.Targets.Where(p => IsSamePath(p, target)).Count() > 1)
                    {
                        return string.Format("The target {0} appears more than once in targets", target.FullName);
                    }
                }

                // The same exception twice
                foreach (var exception in source.Targets)
                {
                    if (source.Exceptions.Where(p => IsSamePath(p, exception)).Count() > 1)
                    {
                        return string.Format("The exception {0} appears more than once in exceptions", exception.FullName);
                    }
                }

                // Any source who is a parent to the current one and not an exception in the parent entry?
                var parentSource = sourceDirectories.FirstOrDefault(p => IsSubDirectoryOfPath(source.DirectoryPath, p.DirectoryPath) && !p.Exceptions.Any(exception => IsSamePath(exception, source.DirectoryPath) || IsSubDirectoryOfPath(source.DirectoryPath, exception)));
                if (parentSource != null)
                {
                    int parentSourceIndex = sourceDirectories.FindIndex(p => p == parentSource);
                    return string.Format("The source {0} {1} has a conflict with source {2} {3}", sourceIndex, source.DirectoryPath, parentSourceIndex, parentSource.DirectoryPath.FullName);
                }

                // Any source who is a child to the current one and not an exception in the current entry?
                var childSource = sourceDirectories.FirstOrDefault(sourceDirectory => IsSubDirectoryOfPath(sourceDirectory.DirectoryPath, source.DirectoryPath) && !source.Exceptions.Any(exception => IsSamePath(exception, sourceDirectory.DirectoryPath) || IsSubDirectoryOfPath(sourceDirectory.DirectoryPath, exception)));
                if (childSource != null)
                {
                    int childSourceIndex = sourceDirectories.FindIndex(p => p == childSource);
                    return string.Format("The source {0} {1} has a conflict with source {2} {3}", sourceIndex, source.DirectoryPath, childSourceIndex, childSource.DirectoryPath.FullName);
                }

                // Check if source exists in targets, or in a subdirectory in one of them, or a target is a sub directory of the new path, also taking in mind exceptions.
                var conflictTarget = sourceDirectories.FirstOrDefault(p =>
                p.Targets.Any(target => IsSamePath(source.DirectoryPath, target) ||
                (IsSubDirectoryOfPath(source.DirectoryPath, target) && !p.Exceptions.Any(l => IsSamePath(source.DirectoryPath, l) || IsSubDirectoryOfPath(source.DirectoryPath, l))) ||
                (IsSubDirectoryOfPath(target, source.DirectoryPath) && !p.Exceptions.Any(exception => IsSamePath(target, exception) || IsSubDirectoryOfPath(target, exception)))));

                if (conflictTarget != null)
                {
                    var conflictTargetIndex = sourceDirectories.FindIndex(p => p == conflictTarget);
                    return string.Format("The source {0} {1} has a conflict with targets in source {2} {3}", sourceIndex, source.DirectoryPath, conflictTargetIndex, conflictTarget);
                }

                // Any same exceptions or exceptions are not child of source?
                var conflictException = source.Exceptions.FirstOrDefault(p => IsSamePath(source.DirectoryPath, p) || !IsSubDirectoryOfPath(p, source.DirectoryPath));

                if (conflictException != null)
                {
                    var conflictExceptionIndex = source.Exceptions.FindIndex(p => p == conflictException);
                    return string.Format("The source {0} {1} has a conflict with exception {2} {3}", conflictExceptionIndex, conflictException);
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether a file system info is in a sub directory of another file system info.
        /// </summary>
        /// <param name="subDirectory">The sub directory.</param>
        /// <param name="path">The path of the parent.</param>
        /// <returns>True if it is a sub directory.</returns>
        public static bool IsSubDirectoryOfPath(FileSystemInfo subDirectory, FileSystemInfo path)
        {
            if (IsSamePath(subDirectory, path) || subDirectory.FullName.Count() <= path.FullName.Count())
            {
                return false;
            }
            else
            {
                return ChangePathToDefaultPath(subDirectory.FullName).Contains(ChangePathToDefaultPath(path.FullName));
            }
        }
    }
}
