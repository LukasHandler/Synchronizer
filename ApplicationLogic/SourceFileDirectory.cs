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

        public bool WatcherCreated { get; set; }

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
            this.WatcherCreated = true;
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

                            jobEntries.Add(new JobEntry(sourceFile, targetFile, eventArgs.ChangeType, oldFile));
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

                            JobEntry entry = new JobEntry(sourceFile, targetFile, eventArgs.ChangeType, oldFile);
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

        public override string ToString()
        {
            return this.Path;
        }
    }
}
