using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class JobManager
    {
        public static ConcurrentQueue<Job> Jobs;

        public static List<Job> ProcessedJobs;

        private static Task jobTask;

        public static object Locker;

        private static bool lastJobFinished;

        static JobManager()
        {
            Locker = new object();
            ProcessedJobs = new List<Job>();
            Jobs = new ConcurrentQueue<Job>();
            lastJobFinished = true;
        }

        public static EventHandler OnJobsChanged;

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
                    OnJobsChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }
    }
}
