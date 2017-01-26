//-----------------------------------------------------------------------
// <copyright file="SourceFileDirectory.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file represents a source file object with the targets and exceptions and the logical to detect changes.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// This class represents a source file object with the targets and exceptions and the logical to detect changes.
    /// </summary>
    [Serializable]
    public class SourceFileDirectory
    {
        /// <summary>
        /// The watcher to detect changes.
        /// </summary>
        [NonSerialized]
        private FileSystemWatcher watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceFileDirectory"/> class.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="recursive">If set to <c>true</c> it is recursive.</param>
        public SourceFileDirectory(DirectoryInfo directoryPath, bool recursive)
        {
            this.DirectoryPath = directoryPath;
            this.Targets = new List<DirectoryInfo>();
            this.Exceptions = new List<DirectoryInfo>();
            this.Recursive = recursive;
        }

        /// <summary>
        /// Gets or sets the directory path.
        /// </summary>
        /// <value>
        /// The directory path.
        /// </value>
        public DirectoryInfo DirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        /// <value>
        /// The targets.
        /// </value>
        public List<DirectoryInfo> Targets { get; set; }

        /// <summary>
        /// Gets or sets the exceptions.
        /// </summary>
        /// <value>
        /// The exceptions.
        /// </value>
        public List<DirectoryInfo> Exceptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SourceFileDirectory"/> is recursive.
        /// </summary>
        /// <value>
        ///   <c>true</c> if recursive; otherwise, <c>false</c>.
        /// </value>
        public bool Recursive { get; set; }

        /// <summary>
        /// Initializes the watcher.
        /// </summary>
        public void InitWatcher()
        {
            if (this.watcher == null && this.DirectoryPath != null)
            {
                this.watcher = new FileSystemWatcher(this.DirectoryPath.FullName);
                this.watcher.Created += this.CreateJob;
                this.watcher.Changed += this.CreateJob;
                this.watcher.Deleted += this.CreateJob;
                this.watcher.Renamed += this.CreateJob;
                this.watcher.IncludeSubdirectories = this.Recursive;
                this.watcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Starts the initial synchronization.
        /// </summary>
        public void InitialSynchronization()
        {
            this.SynchronizeDirectoryFilesRecursiveToTarget(this.DirectoryPath);
        }

        /// <summary>
        /// Synchronizes the directory files recursive to target. If target is null, it creates jobs for all targets.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <param name="targetDirectory">The target directory.</param>
        public void SynchronizeDirectoryFilesRecursiveToTarget(DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory = null)
        {
            this.SynchronizeDirectory(sourceDirectory, targetDirectory, true, false);

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

        /// <summary>
        /// Adds the target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="synchronize">If set to <c>true</c> [synchronize].</param>
        public void AddTarget(DirectoryInfo target, bool synchronize = false)
        {
            this.Targets.Add(target);

            if (synchronize)
            {
                this.SynchronizeDirectoryFilesRecursiveToTarget(this.DirectoryPath, target);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.DirectoryPath.FullName;
        }

        /// <summary>
        /// Synchronizes the directory.
        /// </summary>
        /// <param name="sourceDirectory">The source directory.</param>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="includeFiles">If set to <c>true</c> it includes files.</param>
        /// <param name="createDirectory">If set to <c>true</c> it includes the creation of the directory itself.</param>
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

        /// <summary>
        /// Creates the job for a target.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        private void CreateJobForTarget(FileSystemInfo source, FileSystemInfo target)
        {
            JobEntry entry = new JobEntry(this, source, target, WatcherChangeTypes.Created, null, true);
            Job newJob = new Job(entry);
            JobManager.AddJob(newJob);
        }

        /// <summary>
        /// Creates the job for all targets.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="isDirectory">If set to <c>true</c> it is a directory.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="oldName">The old name.</param>
        /// <param name="isInitial">If set to <c>true</c> it is initial..</param>
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

        /// <summary>
        /// Creates a job.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
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
    }
}
