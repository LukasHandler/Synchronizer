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
        private static ConcurrentBag<Job> ProcessedJobs;

        private static  ConcurrentQueue<Job> Jobs;

        private static Task jobTask;

        static JobManager()
        {
            ProcessedJobs = new ConcurrentBag<Job>();
            Jobs = new ConcurrentQueue<Job>();
        }

        private static void WorkOnJobs()
        {
            bool allJobsCompleted = false;

            while (!allJobsCompleted)
            {
                Job currentJob;
                if (Jobs.TryDequeue(out currentJob))
                {
                    currentJob.JobState = Job.JobStates.Processed;
                    ProcessedJobs.Add(currentJob);
                    currentJob.Execute();
                }
                else
                {
                    // Queue is empty
                    allJobsCompleted = true;
                }
            }
        }

        public static void AddJob(Job newJob)
        {
            Jobs.Enqueue(newJob);

            if (jobTask == null || jobTask.IsCompleted)
            {
                jobTask = new Task(WorkOnJobs);
                jobTask.Start();
            }
        }
    }
}
