using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class ApplicationManager
    {
        private static List<SourceFileDirectory> sourceDirectories;

        public static Settings Settings;

        static ApplicationManager()
        {
            sourceDirectories = new List<SourceFileDirectory>();
            Settings = new Settings();
        }

        public static void LoadSaveFiles()
        {
            try
            {
                sourceDirectories = Serializer.DeSerializeObject<List<SourceFileDirectory>>("SourceDirectories.save");
            }
            catch (Exception exception)
            {
                LoggingManager.Log("Couldn't load SourceDirectories.save: " + exception.Message);
                sourceDirectories = new List<SourceFileDirectory>();
            }

            try
            {
                Settings = Serializer.DeSerializeObject<Settings>("Settings.save");
            }
            catch (Exception exception)
            {
                LoggingManager.Log("Couldn't load Settings.save: " + exception.Message);
                Settings = new Settings();
            }
        }

        public static string ValidateData()
        {
            return PathHelper.IsValid(sourceDirectories, true);
        }

        public static void SaveSettings()
        {
            try
            {
                Serializer.SerializeObject(sourceDirectories, "SourceDirectories.save");
            }
            catch (Exception exception)
            {
                LoggingManager.Log("Couldn't create SourceDirectories.save: " + exception.Message);
            }

            try
            {
                Serializer.SerializeObject(Settings, "Settings.save");
            }
            catch (Exception exception)
            {
                LoggingManager.Log("Couldn't create Settings.save: " + exception.Message);
            }
        }

        public static List<string> GetSources()
        {
            return sourceDirectories.Select(p => p.ToString()).ToList();
        }

        public static string AddSource(string path, bool recursive = false)
        {
            var newSource = new SourceFileDirectory(path, recursive);
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories.Add(newSource);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories.Add(newSource);
            }

            return errorMessage;
        }

        public static void DeleteSource(int sourceId)
        {
            sourceDirectories.RemoveAt(sourceId);
        }

        public static List<string> GetTargets(int sourceId)
        {
            return sourceDirectories[sourceId].Targets.Select(p => p.ToString()).ToList();
        }

        public static string AddTarget(int sourceId, string path)
        {
            var newTarget = new FileDirectory(path);
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories[sourceId].Targets.Add(newTarget);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories[sourceId].AddTarget(newTarget);
            }

            return errorMessage;
        }

        public static void DeleteTarget(int sourceId, int targetId)
        {
            sourceDirectories[sourceId].Targets.RemoveAt(targetId);
        }

        public static List<string> GetExceptions(int sourceId)
        {
            return sourceDirectories[sourceId].Exceptions.Select(p => p.ToString()).ToList();
        }

        public static string AddException(int sourceId, string path)
        {
            var newException = new FileDirectory(path);
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories[sourceId].Exceptions.Add(newException);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories[sourceId].Exceptions.Add(newException);
            }

            return errorMessage;
        }

        public static string DeleteException(int sourceId, int exceptionId)
        {
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories[sourceId].Exceptions.RemoveAt(exceptionId);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories[sourceId].Exceptions.RemoveAt(exceptionId);
            }

            return errorMessage;
        }

        public static List<string> GetJobs()
        {
            List<string> jobs;
            lock (JobManager.Locker)
            {
                jobs = JobManager.ProcessedJobs.Select(p => p.ToString()).Concat(JobManager.Jobs.Select(p => p.ToString())).ToList();
            }

            if (jobs.Count == 0)
            {
                return new List<string>() { "No jobs" };
            }
            else
            {
                return jobs;
            }
        }
    }
}
