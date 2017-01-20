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

                            jobEntries.Add(new JobEntry(this, sourceFile, targetFile, eventArgs.ChangeType, isDirectory, oldFile, false));
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

                            JobEntry entry = new JobEntry(this, sourceFile, targetFile, eventArgs.ChangeType, isDirectory, oldFile, false);
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

        public void InitialSynchronization()
        {
            var directoryPath = PathHelper.ChangePathToDefaultPath(this.Path);

            foreach (var target in this.Targets)
            {
                SynchronizeDirectoryFilesRecursiveToTarget(target.Path, this.Path);
            }
        }

        public void SynchronizeDirectoryFilesRecursiveToTarget(string targetDirectory, string sourceDirectory)
        {
            sourceDirectory = PathHelper.ChangePathToDefaultPath(sourceDirectory);
            targetDirectory = PathHelper.ChangePathToDefaultPath(targetDirectory);
            var directories = Directory.GetDirectories(sourceDirectory, "*.*", SearchOption.AllDirectories);

            SynchronizeDirectory(sourceDirectory, targetDirectory);

            foreach (var directory in directories)
            {
                SynchronizeDirectory(directory, targetDirectory);
            }
        }

        private void SynchronizeDirectory(string directoryPath, string targetDirectory)
        {
            string pathWithoutSource = directoryPath.Substring(this.Path.Count());

            // If directory is not an exception or a subdirectory of an exception, copy directory
            if (!this.Exceptions.Any(p => PathHelper.IsSamePath(p.Path, directoryPath) || PathHelper.IsSubDirectoryOfPath(directoryPath, p.Path)))
            {
                // Create folder in target
                var target = this.Targets.FirstOrDefault(p => PathHelper.IsSamePath(p.Path, targetDirectory) || PathHelper.IsSubDirectoryOfPath(targetDirectory, p.Path));

                if (target == null)
                {
                    return;
                }
                targetDirectory = PathHelper.ChangePathToDefaultPath(target.Path);

                string targetDirectoryPath = System.IO.Path.Combine(targetDirectory, pathWithoutSource);
                if (!Directory.Exists(targetDirectoryPath))
                {
                    JobEntry newJobEntry = new JobEntry(this, new FileInfo(directoryPath), new FileInfo(targetDirectoryPath), WatcherChangeTypes.Created, true, null, true);
                    Job newJob = new Job(newJobEntry);
                    JobManager.AddJob(newJob);
                }

                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    var filePath = PathHelper.ChangePathToDefaultPath(file, true);
                    string filePathWithoutSource = filePath.Substring(this.Path.Count());

                    string targetFilePath = System.IO.Path.Combine(targetDirectory, filePathWithoutSource);
                    if (!File.Exists(targetFilePath))
                    {
                        JobEntry newJobEntry = new JobEntry(this, new FileInfo(filePath), new FileInfo(targetFilePath), WatcherChangeTypes.Created, false, null, true);
                        Job newJob = new Job(newJobEntry);
                        JobManager.AddJob(newJob);
                    }
                }
            }
        }

        //private void SynchronizeDirectory(string directoryPath)
        //{
        //    string pathWithoutSource = directoryPath.Substring(this.Path.Count());

        //    // If directory is not an exception or a subdirectory of an exception, copy directory
        //    if (!this.Exceptions.Any(p => PathHelper.IsSamePath(p.Path, directoryPath) || PathHelper.IsSubDirectoryOfPath(directoryPath, p.Path)))
        //    {
        //        // Create folder in target
        //        foreach (var target in this.Targets)
        //        {
        //            string targetDirectoryPath = System.IO.Path.Combine(target.Path, pathWithoutSource);
        //            if (!Directory.Exists(targetDirectoryPath))
        //            {
        //                JobEntry newJobEntry = new JobEntry(this, new FileInfo(directoryPath), new FileInfo(targetDirectoryPath), WatcherChangeTypes.Created, true, null);
        //                Job newJob = new Job(newJobEntry);
        //                JobManager.AddJob(newJob);
        //            }
        //        }

        //        foreach (var file in Directory.GetFiles(directoryPath))
        //        {
        //            var filePath = PathHelper.ChangePathToDefaultPath(file, true);
        //            string filePathWithoutSource = filePath.Substring(this.Path.Count());

        //            // Create file in target
        //            foreach (var target in this.Targets)
        //            {
        //                string targetFilePath = System.IO.Path.Combine(target.Path, filePathWithoutSource);
        //                if (!File.Exists(targetFilePath))
        //                {
        //                    JobEntry newJobEntry = new JobEntry(this, new FileInfo(filePath), new FileInfo(targetFilePath), WatcherChangeTypes.Created, false, null);
        //                    Job newJob = new Job(newJobEntry);
        //                    JobManager.AddJob(newJob);
        //                }
        //            }
        //        }
        //    }
        //}

        public override string ToString()
        {
            return this.Path;
        }
    }
}
