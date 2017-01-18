using Synchronizer.Shared.EventArguments;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synchronizer.ApplicationLogic
{
    public static class JobManager
    {
        public static ConcurrentQueue<Job> Jobs;

        public static List<Job> ProcessedJobs;

        private static Task jobTask;

        private static object Locker;

        static JobManager()
        {
            Locker = new object();
            ProcessedJobs = new List<Job>();
            Jobs = new ConcurrentQueue<Job>();
        }

        public static EventHandler<LogEventArguments> OnLog;

        private static void WorkOnJobs()
        {
            bool allJobsCompleted = false;

            while (!allJobsCompleted)
            {
                Job currentJob;
                if (Jobs.TryDequeue(out currentJob))
                {
                    currentJob.JobState = Job.JobStates.Processed;
                    lock (Locker)
                    {
                        ProcessedJobs.Add(currentJob);
                    }
                    currentJob.Execute();
                   
                    lock (Locker)
                    {
                        ProcessedJobs.Remove(currentJob);
                    }
                }
                else
                {
                    // Queue is empty
                    allJobsCompleted = true;
                }
            }
        }

        internal static void AddJob(Job newJob)
        {
            Jobs.Enqueue(newJob);

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
    }
}
