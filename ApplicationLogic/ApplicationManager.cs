using Synchronizer.Shared.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public class ApplicationManager
    {
        private List<SourceFileDirectory> sourceDirectories;

        private Settings settings;

        private List<string> logs;

        public ApplicationManager()
        {
            this.logs = new List<string>();

            try
            {
                this.sourceDirectories = Serializer.DeSerializeObject<List<SourceFileDirectory>>("SourceDirectories.save");
            }
            catch (Exception exception)
            {
                this.Log("Couldn't load SourceDirectories.save: " + exception.Message);
                this.sourceDirectories = new List<SourceFileDirectory>();
            }

            try
            {
                this.settings = Serializer.DeSerializeObject<Settings>("Settings.save");
            }
            catch (Exception exception)
            {
                this.Log("Couldn't load Settings.save: " + exception.Message);
                this.settings = new Settings();
            }
        }

        public string ValidateData()
        {
            return PathHelper.IsValid(this.sourceDirectories, true);
        }

        public void SaveSettings()
        {
            try
            {
                Serializer.SerializeObject(this.sourceDirectories, "SourceDirectories.save");
            }
            catch (Exception exception)
            {
                this.Log("Couldn't create SourceDirectories.save: " + exception.Message);
            }

            try
            {
                Serializer.SerializeObject(this.settings, "Settings.save");
            }
            catch (Exception exception)
            {
                this.Log("Couldn't create Settings.save: " + exception.Message);
            }
        }

        public List<string> GetSources()
        {
            return sourceDirectories.Select(p => p.ToString()).ToList();
        }

        public string AddSource(string path, bool recursive = false)
        {
            var newSource = new SourceFileDirectory(path, recursive);
            var newDirectories = Serializer.CopyObject(this.sourceDirectories);
            newDirectories.Add(newSource);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                this.sourceDirectories.Add(newSource);
            }

            return errorMessage;
        }

        public void DeleteSource(int sourceId)
        {
            this.sourceDirectories.RemoveAt(sourceId);
        }

        public List<string> GetTargets(int sourceId)
        {
            return sourceDirectories[sourceId].Targets.Select(p => p.ToString()).ToList();
        }

        public string AddTarget(int sourceId, string path)
        {
            var newTarget = new FileDirectory(path);
            var newDirectories = Serializer.CopyObject(this.sourceDirectories);
            newDirectories[sourceId].Targets.Add(newTarget);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                this.sourceDirectories[sourceId].Targets.Add(newTarget);
            }

            return errorMessage;
        }

        public void DeleteTarget(int sourceId, int targetId)
        {
            this.sourceDirectories[sourceId].Targets.RemoveAt(targetId);
        }

        public List<string> GetExceptions(int sourceId)
        {
            return sourceDirectories[sourceId].Exceptions.Select(p => p.ToString()).ToList();
        }

        public string AddException(int sourceId, string path)
        {
            var newException = new FileDirectory(path);
            var newDirectories = Serializer.CopyObject(this.sourceDirectories);
            newDirectories[sourceId].Exceptions.Add(newException);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                this.sourceDirectories[sourceId].Exceptions.Add(newException);
            }

            return errorMessage;
        }

        public string DeleteException(int sourceId, int exceptionId)
        {
            var newDirectories = Serializer.CopyObject(this.sourceDirectories);
            newDirectories[sourceId].Exceptions.RemoveAt(exceptionId);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                this.sourceDirectories[sourceId].Exceptions.RemoveAt(exceptionId);
            }

            return errorMessage;
        }

        public List<string> GetJobs()
        {
            return JobManager.ProcessedJobs.Select(p => p.ToString()).Concat(JobManager.Jobs.Select(p => p.ToString())).ToList();
        }

        public List<string> GetLogs()
        {
            return this.logs;
        }

        public uint GetBlockCompareMinFileSize()
        {
            return this.settings.BlockCompareMinFileSize;
        }

        public void SetBlockCompareMinFileSize(uint value)
        {
            this.settings.BlockCompareMinFileSize = value;
        }

        public uint GetBlockCompareBlockSize()
        {
            return this.settings.BlockCompareBlockSize;
        }

        public void SetBlockCompareBlockSize(uint value)
        {
            this.settings.BlockCompareBlockSize = value;
        }

        public bool GetParallelSync()
        {
            return this.settings.ParallelSync;
        }

        public void SetParallelSync(bool value)
        {
            this.settings.ParallelSync = value;
        }

        private void Log(string message)
        {
            this.logs.Add(DateTime.Now.ToString("dd.MM.yyyy-HH:mm:ss:FF") + " " + message);
        }
    }
}
