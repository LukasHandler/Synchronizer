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

        public void AddSource(string path)
        {
            this.sourceDirectories.Add(new SourceFileDirectory(path));
        }

        public void DeleteSource(int sourceId)
        {
            this.sourceDirectories.RemoveAt(sourceId);
        }

        public List<string> GetTargets(int sourceId)
        {
            return sourceDirectories[sourceId].TargetDirectories.Select(p => p.ToString()).ToList();
        }

        public void AddTarget(int sourceId, string path)
        {
            this.sourceDirectories[sourceId].TargetDirectories.Add(new FileDirectory(path));
        }

        public List<string> GetExceptions(int sourceId)
        {
            return sourceDirectories[sourceId].Exceptions.Select(p => p.ToString()).ToList();
        }

        public List<string> GetJobs()
        {
            throw new NotImplementedException();
        }

        public List<string> GetLogs()
        {
            return this.logs;
        }

        public string GetSettings()
        {
            return this.settings.ToString();
        }

        private void Log(string message)
        {
            this.logs.Add(DateTime.Now.ToString("dd.MM.yyyy-HH:mm:ss:FF") + " " + message);
        }
    }
}
