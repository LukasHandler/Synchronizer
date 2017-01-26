//-----------------------------------------------------------------------
// <copyright file="JobManager.cs" company="Lukas Handler">
//     Lukas Handler
// </copyright>
// <summary>
// This file represents the job manager.
// </summary>
//-----------------------------------------------------------------------
namespace Synchronizer.ApplicationLogic
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class represents the job manager.
    /// </summary>
    public static class JobManager
    {
        /// <summary>
        /// The job task.
        /// </summary>
        private static Task jobTask;

        /// <summary>
        /// Is the last job finished.
        /// </summary>
        private static bool lastJobFinished;

        /// <summary>
        /// Initializes static members of the <see cref="JobManager"/> class.
        /// </summary>
        static JobManager()
        {
            Locker = new object();
            ProcessedJobs = new List<Job>();
            Jobs = new ConcurrentQueue<Job>();
            lastJobFinished = true;
        }

        /// <summary>
        /// Gets or sets the on jobs changed event.
        /// </summary>
        /// <value>
        /// The on jobs changed event.
        /// </value>
        public static EventHandler OnJobsChanged { get; set; }

        /// <summary>
        /// Gets or sets the queued jobs.
        /// </summary>
        /// <value>
        /// The queued jobs.
        /// </value>
        internal static ConcurrentQueue<Job> Jobs { get; set; }

        /// <summary>
        /// Gets or sets the processed jobs.
        /// </summary>
        /// <value>
        /// The processed jobs.
        /// </value>
        internal static List<Job> ProcessedJobs { get; set; }

        /// <summary>
        /// Gets or sets the locker.
        /// </summary>
        /// <value>
        /// The locker.
        /// </value>
        internal static object Locker { get; set; }

        /// <summary>
        /// Determines whether all jobs are finished or not.
        /// </summary>
        /// <returns>True if all jobs have finished.</returns>
        public static bool HasFinished()
        {
            if (Jobs == null || Jobs.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the job.
        /// </summary>
        /// <param name="newJob">The new job.</param>
        internal static void AddJob(Job newJob)
        {
            lock (Locker)
            {
                Jobs.Enqueue(newJob);
            }

            OnJobsChanged?.Invoke(null, EventArgs.Empty);

            if (jobTask == null || jobTask.IsCompleted)
            {
                jobTask = new Task(WorkOnJobs);
                jobTask.Start();
            }
        }

        /// <summary>
        /// Gets called when a job entry got finished.
        /// </summary>
        /// <param name="jobEntry">The job entry.</param>
        internal static void FinishedJobEntry(JobEntry jobEntry)
        {
            lock (Locker)
            {
                var job = ProcessedJobs.FirstOrDefault(p => p.LinkedJobs.Contains(jobEntry));

                if (job != null)
                {
                    job.LinkedJobs.Remove(jobEntry);
                    OnJobsChanged?.Invoke(null, EventArgs.Empty);
                }

                if (job.LinkedJobs.Count == 0)
                {
                    ProcessedJobs.Remove(job);
                    lastJobFinished = true;

                    if (ProcessedJobs.Count == 0)
                    {
                        OnJobsChanged?.Invoke(null, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Works the on jobs.
        /// </summary>
        private static void WorkOnJobs()
        {
            bool allJobsCompleted = false;

            while (!allJobsCompleted)
            {
                if (lastJobFinished)
                {
                    Job currentJob;
                    bool canDequeue = false;

                    lock (Locker)
                    {
                        canDequeue = Jobs.TryDequeue(out currentJob);
                    }

                    if (canDequeue)
                    {
                        currentJob.JobState = Job.JobStates.Processed;
                        lock (Locker)
                        {
                            ProcessedJobs.Add(currentJob);
                        }

                        lastJobFinished = false;
                        OnJobsChanged?.Invoke(null, EventArgs.Empty);
                        currentJob.Execute();
                    }
                    else
                    {
                        // Queue is empty
                        allJobsCompleted = true;
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
