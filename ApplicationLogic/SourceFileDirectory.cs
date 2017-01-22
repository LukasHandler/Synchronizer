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

        [NonSerialized()]
        private FileSystemWatcher watcher;

        public SourceFileDirectory(string directoryPath, bool recursive) : base(directoryPath)
        {
            this.Targets = new List<FileDirectory>();
            this.Exceptions = new List<FileDirectory>();
            this.InitWatcher();
        }

        public void InitWatcher()
        {
            if (watcher == null && !string.IsNullOrEmpty(this.Path))
            {
                watcher = new FileSystemWatcher(this.Path);
                watcher.Created += CreateJob;
                watcher.Changed += CreateJob;
                watcher.Deleted += CreateJob;
                watcher.Renamed += CreateJob;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
            }
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

                    if (canSynchronize && ApplicationManager.Settings.ParallelSync)
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

        public void InitialSynchronization()
        {
            var directoryPath = PathHelper.ChangePathToDefaultPath(this.Path);
            this.SynchronizeDirectoryFilesRecursiveToTarget(directoryPath);
        }

        public void SynchronizeDirectoryFilesRecursiveToTarget(string sourceDirectory, string targetDirectory = null)
        {
            sourceDirectory = PathHelper.ChangePathToDefaultPath(sourceDirectory);
            var directories = Directory.GetDirectories(sourceDirectory, "*.*", SearchOption.AllDirectories);

            if (targetDirectory != null)
            {
                targetDirectory = PathHelper.ChangePathToDefaultPath(targetDirectory);
            }

            SynchronizeDirectory(sourceDirectory, targetDirectory);
            directories.ToList().ForEach(p => this.SynchronizeDirectory(p, targetDirectory));
        }

        private void SynchronizeDirectory(string sourceDirectory, string targetDirectory = null)
        {
            string pathWithoutSource = sourceDirectory.Substring(this.Path.Count());

            // If directory is not an exception or a subdirectory of an exception, copy directory
            if (!this.Exceptions.Any(p => PathHelper.IsSamePath(p.Path, sourceDirectory) || PathHelper.IsSubDirectoryOfPath(sourceDirectory, p.Path)))
            {
                // Get the target directory
                if (targetDirectory != null)
                {
                    var realTarget = this.Targets.FirstOrDefault(p => PathHelper.IsSamePath(p.Path, targetDirectory) || PathHelper.IsSubDirectoryOfPath(targetDirectory, p.Path));
                    if (realTarget == null)
                    {
                        return;
                    }
                }

                // Create folder jobs
                List<JobEntry> jobEntriesFolder = new List<JobEntry>();
                string directoryPathWithoutSource = sourceDirectory.Substring(this.Path.Count());

                if (targetDirectory == null)
                {
                    this.Targets.ForEach(target => this.AddTargetDirectoryJob(jobEntriesFolder, sourceDirectory, System.IO.Path.Combine(target.Path, directoryPathWithoutSource)));
                }
                else
                {
                    this.AddTargetDirectoryJob(jobEntriesFolder, sourceDirectory, System.IO.Path.Combine(targetDirectory, directoryPathWithoutSource));
                }

                if (jobEntriesFolder.Count != 0)
                {
                    Job newJob = new Job(jobEntriesFolder);
                    JobManager.AddJob(newJob);
                }

                // Create files jobs
                foreach (var file in Directory.GetFiles(sourceDirectory))
                {
                    var filePath = PathHelper.ChangePathToDefaultPath(file, true);
                    string filePathWithoutSource = filePath.Substring(this.Path.Count());

                    List<JobEntry> jobEntriesFile = new List<JobEntry>();

                    if (targetDirectory == null)
                    {
                        this.Targets.ForEach(target => this.AddTargetFileJob(jobEntriesFile, filePath, System.IO.Path.Combine(PathHelper.ChangePathToDefaultPath(target.Path), filePathWithoutSource)));
                    }
                    else
                    {
                        string targetFilePath = System.IO.Path.Combine(PathHelper.ChangePathToDefaultPath(targetDirectory), filePathWithoutSource);
                        AddTargetFileJob(jobEntriesFile, filePath, targetFilePath);
                    }

                    if (jobEntriesFile.Count != 0)
                    {
                        Job newJob = new Job(jobEntriesFile);
                        JobManager.AddJob(newJob);

                    }
                }
            }
        }

        private void AddTargetDirectoryJob(List<JobEntry> jobEntries, string sourceDirectory, string targetDirectory)
        {
            targetDirectory = PathHelper.ChangePathToDefaultPath(targetDirectory, false);
            jobEntries.Add(new JobEntry(this, new FileInfo(sourceDirectory), new FileInfo(targetDirectory), WatcherChangeTypes.Created, true, null, true));
        }

        private void AddTargetFileJob(List<JobEntry> jobEntries, string sourceFile, string targetFile)
        {
            targetFile = PathHelper.ChangePathToDefaultPath(targetFile, true);
            jobEntries.Add(new JobEntry(this, new FileInfo(sourceFile), new FileInfo(targetFile), WatcherChangeTypes.Created, false, null, true));
        }

        public void AddTarget(FileDirectory target, bool synchronize = false)
        {
            this.Targets.Add(target);

            if (synchronize)
            {
                this.SynchronizeDirectoryFilesRecursiveToTarget(target.Path, this.Path);
            }
        }

        public void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.Dispose();
            }
        }

        public override string ToString()
        {
            return this.Path;
        }
    }
}
