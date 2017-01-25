using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    [Serializable()]
    public class SourceFileDirectory
    {
        public DirectoryInfo DirectoryPath { get; set; }

        public List<DirectoryInfo> Targets { get; set; }

        public List<DirectoryInfo> Exceptions { get; set; }

        public bool Recursive { get; set; }

        [NonSerialized()]
        private FileSystemWatcher watcher;

        public SourceFileDirectory(DirectoryInfo directoryPath, bool recursive)
        {
            this.DirectoryPath = directoryPath;
            this.Targets = new List<DirectoryInfo>();
            this.Exceptions = new List<DirectoryInfo>();
            this.Recursive = recursive;
        }

        public void InitWatcher()
        {
            if (watcher == null && this.DirectoryPath != null)
            {
                watcher = new FileSystemWatcher(this.DirectoryPath.FullName);
                watcher.Created += CreateJob;
                watcher.Changed += CreateJob;
                watcher.Deleted += CreateJob;
                watcher.Renamed += CreateJob;
                watcher.IncludeSubdirectories = this.Recursive;
                watcher.EnableRaisingEvents = true;
            }
        }

        private void CreateJob(object sender, FileSystemEventArgs eventArgs)
        {
            FileInfo sourceFile = new FileInfo(eventArgs.FullPath);

            if (!this.Exceptions.Any(exception => PathHelper.IsSamePath(exception, sourceFile) || PathHelper.IsSubDirectoryOfPath(sourceFile.Directory, exception)))
            {
                if (this.Targets != null && this.Targets.Count > 0)
                {
                    string oldName = string.Empty;

                    if (eventArgs.ChangeType == WatcherChangeTypes.Renamed)
                    {
                        RenamedEventArgs renamedArgs = (RenamedEventArgs)eventArgs;
                        oldName = renamedArgs.OldName;
                    }

                    this.CreateJobForAllTargets(sourceFile, false, eventArgs.ChangeType, oldName, false);

                }
            }
        }

        public void InitialSynchronization()
        {
            this.SynchronizeDirectoryFilesRecursiveToTarget(this.DirectoryPath);
        }

        public void SynchronizeDirectoryFilesRecursiveToTarget(DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory = null)
        {
            SynchronizeDirectory(sourceDirectory, targetDirectory, true, false);

            DirectoryInfo[] directories;

            if (this.Recursive)
            {
                directories = sourceDirectory.GetDirectories("*.*", SearchOption.AllDirectories);
                directories.ToList().ForEach(p => this.SynchronizeDirectory(p, targetDirectory));

            }
            else
            {
                directories = sourceDirectory.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
                directories.ToList().ForEach(p => this.SynchronizeDirectory(p, targetDirectory, false));
            }

        }

        private void SynchronizeDirectory(DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory = null, bool includeFiles = true, bool createDirectory = true)
        {
            // If directory is not an exception or a subdirectory of an exception, copy directory
            if (!this.Exceptions.Any(p => PathHelper.IsSamePath(p, sourceDirectory) || PathHelper.IsSubDirectoryOfPath(sourceDirectory, p)))
            {
                DirectoryInfo targetRoot = null;
                // Get the target directory
                if (targetDirectory != null)
                {
                    targetRoot = this.Targets.FirstOrDefault(target => PathHelper.IsSamePath(target, targetDirectory) || PathHelper.IsSubDirectoryOfPath(targetDirectory, target));
                    if (targetRoot == null)
                    {
                        return;
                    }
                }

                if (createDirectory)
                {
                    // Create folder jobs
                    if (targetDirectory == null)
                    {
                        this.CreateJobForAllTargets(sourceDirectory, true, WatcherChangeTypes.Created, string.Empty, true);
                    }
                    else
                    {
                        this.CreateJobForTarget(sourceDirectory, targetRoot);
                    }
                }

                // Create files jobs
                if (includeFiles)
                {
                    foreach (var file in Directory.GetFiles(sourceDirectory.FullName))
                    {
                        var filePath = new FileInfo(file);

                        List<JobEntry> jobEntriesFile = new List<JobEntry>();

                        if (targetDirectory == null)
                        {
                            this.CreateJobForAllTargets(filePath, false, WatcherChangeTypes.Created, string.Empty, true);
                        }
                        else
                        {
                            this.CreateJobForTarget(filePath, targetRoot);
                        }
                    }
                }
            }
        }

        private void CreateJobForTarget(FileSystemInfo source, FileSystemInfo target)
        {
            JobEntry entry = new JobEntry(this, source, target, WatcherChangeTypes.Created, null, true);
            Job newJob = new Job(entry);
            JobManager.AddJob(newJob);
        }

        private void CreateJobForAllTargets(FileSystemInfo source, bool isDirectory, WatcherChangeTypes operation, string oldName, bool isInitial)
        {
            FileSystemInfo oldFile;
            bool parallel = ApplicationManager.Settings.ParallelSync && PathHelper.CanSynchronize(this.Targets);
            string directoryPathWithoutSource = source.FullName.Substring(this.DirectoryPath.FullName.Count());

            // Parallel
            List<JobEntry> jobEntries = new List<JobEntry>();

            foreach (var target in this.Targets)
            {
                oldFile = new FileInfo(Path.Combine(target.FullName, oldName));
                var targetFile = new FileInfo(Path.Combine(target.FullName, directoryPathWithoutSource));

                JobEntry entry = new JobEntry(this, source, targetFile, operation, oldFile, isInitial);

                if (parallel)
                {
                    jobEntries.Add(entry);
                }
                else
                {
                    Job newJob = new Job(entry);
                    JobManager.AddJob(newJob);
                }
            }

            if (parallel && jobEntries.Count != 0)
            {
                Job newJob = new Job(jobEntries);
                JobManager.AddJob(newJob);
            }
        }

        public void AddTarget(DirectoryInfo target, bool synchronize = false)
        {
            this.Targets.Add(target);

            if (synchronize)
            {
                this.SynchronizeDirectoryFilesRecursiveToTarget(this.DirectoryPath, target);
            }
        }

        public void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
            }
        }

        public override string ToString()
        {
            return this.DirectoryPath.FullName;
        }
    }
}
