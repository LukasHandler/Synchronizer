using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    [Serializable()]
    public class SourceFileDirectory : FileDirectory
    {
        public List<FileDirectory> Targets { get; set; }

        public List<FileDirectory> Exceptions { get; set; }

        public SourceFileDirectory(string directoryPath, bool recursive) : base(directoryPath)
        {
            this.Targets = new List<FileDirectory>();
            this.Exceptions = new List<FileDirectory>();
            InitWatcher();
        }

        public void InitWatcher()
        {
            var watcher = new FileSystemWatcher(this.Path);
            watcher.Created += CreateJob;
            watcher.Changed += CreateJob;
            watcher.Deleted += CreateJob;
            watcher.Renamed += CreateJob;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void CreateJob(object sender, FileSystemEventArgs eventArgs)
        {
            FileInfo sourceFile = new FileInfo(PathHelper.ChangePathToDefaultPath(eventArgs.FullPath, true));

            if (!this.Exceptions.Any(exception => PathHelper.IsSamePath(exception.Path, sourceFile.Directory.FullName) || PathHelper.IsSubDirectoryOfPath(sourceFile.Directory.FullName, exception.Path)))
            {
                if (this.Targets != null && this.Targets.Count > 0)
                {
                    List<string> pathes = this.Targets.Select(p => p.Path).ToList();
                    bool canSynchronize = PathHelper.CanSynchronize(pathes);

                    FileInfo oldFile = null;

                    if (canSynchronize)
                    {
                        List<JobEntry> jobEntries = new List<JobEntry>();

                        foreach (var target in Targets)
                        {
                            string pathWithoutSource = sourceFile.FullName.Substring(this.Path.Count());
                            FileInfo targetFile = new FileInfo(System.IO.Path.Combine(target.Path, pathWithoutSource));

                            if (eventArgs.ChangeType == WatcherChangeTypes.Renamed)
                            {
                                RenamedEventArgs renamedArgs = (RenamedEventArgs)eventArgs;
                                oldFile = new FileInfo(System.IO.Path.Combine(target.Path, renamedArgs.OldName));
                            }

                            bool isDirectory;
                            if (eventArgs.ChangeType != WatcherChangeTypes.Deleted)
                            {
                                isDirectory = sourceFile.Attributes.HasFlag(FileAttributes.Directory);
                            }
                            else
                            {
                                isDirectory = targetFile.Attributes.HasFlag(FileAttributes.Directory);
                            }

                            jobEntries.Add(new JobEntry(sourceFile, targetFile, eventArgs.ChangeType, isDirectory, oldFile));
                        }

                        Job newJob = new Job(jobEntries);
                        JobManager.AddJob(newJob);
                    }
                    else
                    {
                        foreach (var target in Targets)
                        {
                            string pathWithoutSource = sourceFile.FullName.Substring(this.Path.Count());
                            FileInfo targetFile = new FileInfo(System.IO.Path.Combine(target.Path, pathWithoutSource));

                            if (eventArgs.ChangeType == WatcherChangeTypes.Renamed)
                            {
                                RenamedEventArgs renamedArgs = (RenamedEventArgs)eventArgs;
                                oldFile = new FileInfo(System.IO.Path.Combine(target.Path, renamedArgs.OldName));
                            }

                            bool isDirectory;
                            if (eventArgs.ChangeType != WatcherChangeTypes.Deleted)
                            {
                                isDirectory = sourceFile.Attributes.HasFlag(FileAttributes.Directory);
                            }
                            else
                            {
                                isDirectory = targetFile.Attributes.HasFlag(FileAttributes.Directory);
                            }

                            JobEntry entry = new JobEntry(sourceFile, targetFile, eventArgs.ChangeType, isDirectory, oldFile);
                            Job newJob = new Job(entry);
                            JobManager.AddJob(newJob);
                        }
                    }
                }
            }
        }

        private SourceFileDirectory()
        {

        }

        public void SynchronizeDirectoryRecursive(string directoryPath = null)
        {
            if (directoryPath == null)
            {
                directoryPath = this.Path;
            }

            // Create files in current directory
            SynchronizeDirectory(PathHelper.ChangePathToDefaultPath(directoryPath));

            // Synchronize resursive
            var directories = Directory.GetDirectories(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach (var directory in directories)
            {
                var directoryPathDefaultPath = PathHelper.ChangePathToDefaultPath(directory);
                SynchronizeDirectory(directoryPathDefaultPath);
            }
        }

        private void SynchronizeDirectory(string directoryPath)
        {
            string pathWithoutSource = directoryPath.Substring(this.Path.Count());

            // If directory is not an exception or a subdirectory of an exception, copy directory
            if (!this.Exceptions.Any(p => PathHelper.IsSamePath(p.Path, directoryPath) || PathHelper.IsSubDirectoryOfPath(directoryPath, p.Path)))
            {
                // Create folder in target
                foreach (var target in this.Targets)
                {
                    string targetDirectoryPath = System.IO.Path.Combine(target.Path, pathWithoutSource);
                    if (!Directory.Exists(targetDirectoryPath))
                    {
                        JobEntry newJobEntry = new JobEntry(new FileInfo(directoryPath), new FileInfo(targetDirectoryPath), WatcherChangeTypes.Created, true, null);
                        Job newJob = new Job(newJobEntry);
                        JobManager.AddJob(newJob);
                    }
                }

                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    var filePath = PathHelper.ChangePathToDefaultPath(file, true);
                    string filePathWithoutSource = filePath.Substring(this.Path.Count());

                    // Create file in target
                    foreach (var target in this.Targets)
                    {
                        string targetFilePath = System.IO.Path.Combine(target.Path, filePathWithoutSource);
                        if (!File.Exists(targetFilePath))
                        {
                            JobEntry newJobEntry = new JobEntry(new FileInfo(filePath), new FileInfo(targetFilePath), WatcherChangeTypes.Created, false, null);
                            Job newJob = new Job(newJobEntry);
                            JobManager.AddJob(newJob);
                        }
                    }
                }
            }
        }

        private void CopyAttributes(string sourcePath, string targetPath, bool isFile)
        {
            FileSystemInfo source;
            FileSystemInfo target;

            if (isFile)
            {
                source = new FileInfo(sourcePath);
                target = new FileInfo(targetPath);
            }
            else
            {
                source = new DirectoryInfo(sourcePath);
                target = new DirectoryInfo(targetPath);
            }

            target.Attributes = source.Attributes;
            target.CreationTime = source.CreationTime;
            target.CreationTimeUtc = source.CreationTimeUtc;
            target.LastWriteTime = source.LastWriteTime;
            target.LastWriteTimeUtc = source.LastWriteTimeUtc;
            target.LastAccessTime = source.LastAccessTime;
            target.LastAccessTimeUtc = source.LastAccessTimeUtc;

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
        }

        public override string ToString()
        {
            return this.Path;
        }
    }
}
