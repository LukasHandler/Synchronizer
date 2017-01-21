using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class PathHelper
    {
        public enum SourceErrors
        {
            Valid,
            SourceAlreadyExists,
        }

        public static bool IsSamePath(string path1, string path2)
        {
            if (ChangePathToDefaultPath(path1) == ChangePathToDefaultPath(path2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string RemoveFileFromDirectoryPath(string full, string toDelete)
        {
            // http://stackoverflow.com/questions/2201595/c-sharp-simplest-way-to-remove-first-occurance-of-a-substring-from-another-str
            int index = full.IndexOf(toDelete);
            return PathHelper.ChangePathToDefaultPath(full.Remove(index, toDelete.Length));
        }

        public static string ChangePathToDefaultPath(string path, bool isFile = false)
        {
            string newPath = path.Replace('/', '\\').Trim().TrimEnd('\\');

            if (isFile)
            {
                var objectName = newPath.Split('\\').Last();
                int index = newPath.IndexOf(objectName);
                string dictionaryPath = PathHelper.ChangePathToDefaultPath(newPath.Remove(index, objectName.Length));
                newPath = dictionaryPath.ToLower() + objectName;
            }
            else
            {
                newPath = newPath.ToLower();
                newPath += '\\';
            }

            return newPath;
        }

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

        public static bool CanSynchronize(List<string> pathes)
        {
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

        // Validate whole structure.
        public static string IsValid(List<SourceFileDirectory> sourceDirectories, bool onStart = false)
        {
            // Validate all pathes and change the layout.
            foreach (var source in sourceDirectories)
            {
                // Check source.
                if (!Directory.Exists(source.Path))
                {
                    int sourceIndex = sourceDirectories.FindIndex(p => p == source);
                    return string.Format("Source {0} {1} doesn't exist", sourceIndex, source.Path);
                }
                else
                {
                    source.Path = ChangePathToDefaultPath(source.Path);
                }

                // Check all targets.
                foreach (var target in source.Targets)
                {
                    if (!Directory.Exists(target.Path))
                    {
                        int targetIndex = sourceDirectories.FindIndex(p => p == target);
                        return string.Format("Target {0} {1} doesn't exist", targetIndex, target.Path);
                    }
                    else
                    {
                        target.Path = ChangePathToDefaultPath(target.Path);
                    }
                }

                // Check all exceptions.
                foreach (var exception in source.Exceptions)
                {
                    if (!Directory.Exists(exception.Path))
                    {
                        int exceptionIndex = sourceDirectories.FindIndex(p => p == exception);
                        return string.Format("Exception {0} {1} doesn't exist", exceptionIndex, exception.Path);
                    }
                    else
                    {
                        exception.Path = ChangePathToDefaultPath(exception.Path);
                    }
                }

                if (onStart)
                {
                    source.InitWatcher();
                }
            }

            // Validate each source
            foreach (var source in sourceDirectories)
            {
                int sourceIndex = sourceDirectories.FindIndex(p => p == source);

                // Aagainst the other sources.
                // The same source twice?
                if (sourceDirectories.Where(p => IsSamePath(p.Path, source.Path)).Count() > 1)
                {
                    return string.Format("The source {0} appears more than once in sources", source.Path);
                }

                // The same target twice
                foreach (var target in source.Targets)
                {
                    if (source.Targets.Where(p => IsSamePath(p.Path, target.Path)).Count() > 1)
                    {
                        return string.Format("The target {0} appears more than once in targets", target.Path);
                    }
                }

                // The same exception twice
                foreach (var exception in source.Targets)
                {
                    if (source.Exceptions.Where(p => IsSamePath(p.Path, exception.Path)).Count() > 1)
                    {
                        return string.Format("The exception {0} appears more than once in exceptions", exception.Path);
                    }
                }

                // Any source who is a parent to the current one and not an exception in the parent entry?
                var parentSource = sourceDirectories.FirstOrDefault(p => IsSubDirectoryOfPath(source.Path, p.Path) && !p.Exceptions.Any(exception => IsSamePath(exception.Path, source.Path) || IsSubDirectoryOfPath(source.Path, exception.Path)));
                if (parentSource != null)
                {
                    int parentSourceIndex = sourceDirectories.FindIndex(p => p == parentSource);
                    return string.Format("The source {0} {1} has a conflict with source {2} {3}", sourceIndex, source.Path, parentSourceIndex, parentSource.Path);
                }

                // Any source who is a child to the current one and not an exception in the current entry?
                var childSource = sourceDirectories.FirstOrDefault(p => IsSubDirectoryOfPath(p.Path, source.Path) && !source.Exceptions.Any(x => IsSamePath(x.Path, p.Path) || IsSubDirectoryOfPath(p.Path, x.Path)));
                if (childSource != null)
                {
                    int childSourceIndex = sourceDirectories.FindIndex(p => p == childSource);
                    return string.Format("The source {0} {1} has a conflict with source {2} {3}", sourceIndex, source.Path, childSourceIndex, childSource.Path);
                }

                // Check if source exists in targets, or in a subdirectory in one of them, or a target is a sub directory of the new path, also taking in mind exceptions.
                var conflictTarget = sourceDirectories.FirstOrDefault(p =>
                p.Targets.Any(target => IsSamePath(source.Path, target.Path) ||
                (IsSubDirectoryOfPath(source.Path, target.Path) && !p.Exceptions.Any(l => IsSamePath(source.Path, l.Path) || IsSubDirectoryOfPath(source.Path, l.Path))) ||
                (IsSubDirectoryOfPath(target.Path, source.Path) && !p.Exceptions.Any(l => IsSamePath(target.Path, l.Path) || IsSubDirectoryOfPath(target.Path, l.Path)))));

                if (conflictTarget != null)
                {
                    var conflictTargetIndex = sourceDirectories.FindIndex(p => p == conflictTarget);
                    return string.Format("The source {0} {1} has a conflict with targets in source {2} {3}", sourceIndex, source.Path, conflictTargetIndex, conflictTarget.Path);
                }

                // Any same exceptions or exceptions are not child of source?
                var conflictException = source.Exceptions.FirstOrDefault(p => IsSamePath(source.Path, p.Path) || !IsSubDirectoryOfPath(p.Path, source.Path));

                if (conflictException != null)
                {
                    var conflictExceptionIndex = source.Exceptions.FindIndex(p => p == conflictException);
                    return string.Format("The source {0} {1} has a conflict with exception {2} {3}", conflictExceptionIndex, conflictException.Path);
                }
            }

            // Synchronize sources
            foreach (var sourceDirectory in sourceDirectories)
            {
                sourceDirectory.InitialSynchronization();
            }

            return null;
        }

        public static string IsValidSource(string path, List<SourceFileDirectory> sourceDirectories)
        {
            // Check if source already exists
            if (sourceDirectories.Any(p => IsSamePath(path, p.Path)))
            {
                return "Source already exists";
            }

            // Check if path is a sub directory of a existing source and not an exception
            var parentDirectories = sourceDirectories.Where(p => IsSubDirectoryOfPath(path, p.Path));
            if (parentDirectories != null)
            {
                // Check if each of the parent directories have the new path in the exceptions
                foreach (var item in parentDirectories)
                {
                    if (!item.Exceptions.Any(p => IsSamePath(path, p.Path) || IsSubDirectoryOfPath(path, p.Path)))
                    {
                        var sourceIndex = sourceDirectories.FindIndex(p => p == item);
                        return string.Format("Path is a subdirectory of source {0} {1}, but not an exception", sourceIndex, item.Path);
                    }
                }
            }

            // Check if source exists in targets, or in a subdirectory in one of them, or a target is a sub directory of the new path, also taking in mind exceptions.
            var conflictTarget = sourceDirectories.FirstOrDefault(p =>
            p.Targets.Any(a => IsSamePath(path, a.Path) ||
            (IsSubDirectoryOfPath(path, a.Path) && !p.Exceptions.Any(l => IsSamePath(path, l.Path) || IsSubDirectoryOfPath(path, l.Path))) ||
            (IsSubDirectoryOfPath(a.Path, path) && !p.Exceptions.Any(l => IsSamePath(a.Path, l.Path) || IsSubDirectoryOfPath(a.Path, l.Path)))));

            if (conflictTarget != null)
            {
                var sourceIndex = sourceDirectories.FindIndex(p => p == conflictTarget);
                return string.Format("Conflict with new path and targets in source {0} {1}", sourceIndex, conflictTarget.Path);
            }

            return string.Empty;
        }

        public static bool IsSubDirectoryOfPath(string subDirectory, string path)
        {
            if (IsSamePath(subDirectory, path) || subDirectory.Count() <= path.Count())
            {
                return false;
            }
            else
            {
                return ChangePathToDefaultPath(subDirectory).Contains(ChangePathToDefaultPath(path));
            }
        }
    }
}
