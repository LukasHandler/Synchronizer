//-----------------------------------------------------------------------
// <copyright file="ApplicationManager.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file holds our important data and makes them accessible for the presentation layer.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// This class holds our important data and makes them accessible for the presentation layer.
    /// </summary>
    public static class ApplicationManager
    {
        /// <summary>
        /// The source directories.
        /// </summary>
        private static List<SourceFileDirectory> sourceDirectories;

        /// <summary>
        /// Initializes static members of the <see cref="ApplicationManager"/> class.
        /// </summary>
        static ApplicationManager()
        {
            sourceDirectories = new List<SourceFileDirectory>();
            Settings = new Settings();
        }

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public static Settings Settings { get; set; }

        /// <summary>
        /// Loads the save files.
        /// </summary>
        /// <returns>A possible error message if the loaded structure is invalid.</returns>
        public static string LoadSaveFiles()
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

            string errorMessage = PathHelper.IsValid(sourceDirectories);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories = new List<SourceFileDirectory>();
            }

            return errorMessage;
        }

        /// <summary>
        /// Starts the initial synchronization.
        /// </summary>
        public static void InitialSynchronization()
        {
            foreach (var source in sourceDirectories)
            {
                source.InitWatcher();
                source.InitialSynchronization();
            }
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
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

        /// <summary>
        /// Gets the sources.
        /// </summary>
        /// <returns>A textual representation of the sources.</returns>
        public static List<string> GetSources()
        {
            return sourceDirectories.Select(p => p.ToString() + "    Recursive: " + p.Recursive.ToString()).ToList();
        }

        /// <summary>
        /// Adds a source.
        /// </summary>
        /// <param name="path">The path of the source.</param>
        /// <param name="recursive">If set to <c>true</c> the source is recursive.</param>
        /// <returns>A possible error message if the source couldn't be added.</returns>
        public static string AddSource(string path, bool recursive)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            var newSource = new SourceFileDirectory(directoryInfo, recursive);
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories.Add(newSource);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories.Add(newSource);
                newSource.InitWatcher();
            }
            else
            {
                newSource.Dispose();
            }

            return errorMessage;
        }

        /// <summary>
        /// Deletes the source.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        public static void DeleteSource(int sourceId)
        {
            var sourceToDelete = sourceDirectories.ElementAt(sourceId);
            sourceToDelete.Dispose();
            sourceDirectories.Remove(sourceToDelete);
        }

        /// <summary>
        /// Gets the targets.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <returns>A textual representation of the targets.</returns>
        public static List<string> GetTargets(int sourceId)
        {
            return sourceDirectories[sourceId].Targets.Select(p => p.ToString()).ToList();
        }

        /// <summary>
        /// Adds the target.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <param name="path">The path of the new target.</param>
        /// <param name="startSynchronization">If set to <c>true</c> the target should synchronize the content of the source.</param>
        /// <returns>A possible error message if the target couldn't be added.</returns>
        public static string AddTarget(int sourceId, string path, bool startSynchronization = true)
        {
            var newTarget = new DirectoryInfo(path);
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories[sourceId].AddTarget(newTarget);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories[sourceId].AddTarget(newTarget, startSynchronization);
            }

            return errorMessage;
        }

        /// <summary>
        /// Deletes the target.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <param name="targetId">The target identifier.</param>
        public static void DeleteTarget(int sourceId, int targetId)
        {
            sourceDirectories[sourceId].Targets.RemoveAt(targetId);
        }

        /// <summary>
        /// Gets the exceptions.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <returns>A textual representation of the exceptions.</returns>
        public static List<string> GetExceptions(int sourceId)
        {
            return sourceDirectories[sourceId].Exceptions.Select(p => p.ToString()).ToList();
        }

        /// <summary>
        /// Adds the exception.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <param name="path">The path for the new exception.</param>
        /// <returns>A possible error message if the exception couldn't be added.</returns>
        public static string AddException(int sourceId, string path)
        {
            var newException = new DirectoryInfo(path);
            var newDirectories = Serializer.CopyObject(sourceDirectories);
            newDirectories[sourceId].Exceptions.Add(newException);
            var errorMessage = PathHelper.IsValid(newDirectories);

            if (string.IsNullOrEmpty(errorMessage))
            {
                sourceDirectories[sourceId].Exceptions.Add(newException);
            }

            return errorMessage;
        }

        /// <summary>
        /// Deletes the exception.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <param name="exceptionId">The exception identifier.</param>
        /// <returns>A possible error message if the exception couldn't be added.</returns>
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

        /// <summary>
        /// Gets the current jobs.
        /// </summary>
        /// <returns>A textual representation of the current jobs.</returns>
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
