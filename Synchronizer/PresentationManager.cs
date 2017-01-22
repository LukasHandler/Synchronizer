using Synchronizer.ApplicationLogic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace Synchronizer.PresentationLogic
{
    public class PresentationManager
    {
        private ConcurrentBag<string> Logs;

        private ConcurrentBag<string> JobLogs;

        private Process loggingProcess;

        private enum Menu
        {
            Sources,
            Targets,
            Exceptions,
            Jobs,
            Logs,
            Settings,
            Help
        }

        private enum IdOperation
        {
            DeleteSource,
            DeleteTarget,
            DeleteException,
            GetTargets,
            GetExceptions
        }

        private enum PathOperation
        {
            Source,
            Target,
            Exception
        }

        private Dictionary<string, Menu> menus;

        private Menu currentMenu;

        private int? currentSourceId;

        public PresentationManager(string[] args)
        {
            this.menus = new Dictionary<string, Menu>();
            this.menus.Add("F3", Menu.Sources);
            this.menus.Add("F4", Menu.Targets);
            this.menus.Add("F5", Menu.Exceptions);
            this.menus.Add("F6", Menu.Jobs);
            this.menus.Add("F7", Menu.Logs);
            this.menus.Add("F8", Menu.Settings);
            this.menus.Add("F9", Menu.Help);

            this.Logs = new ConcurrentBag<string>();
            this.JobLogs = new ConcurrentBag<string>();

            JobManager.OnJobsChanged += RefreshJobs;

            this.currentSourceId = 0;

            bool validArguments = this.ValidateArguments(args);

            try
            {
                loggingProcess = Process.Start("LogPresentationLogic.exe");
            }
            catch (Exception exception)
            {
                LoggingManager.Log("Logging window couldn't start: " + exception.Message);
            }


            if (!validArguments)
            {
                ApplicationManager.LoadSaveFiles();
            }
        }

        private bool ValidateArguments(string[] args)
        {
            string helpMessage = string.Format(
                @"Help for synchronizer application:\r\n" +
                "The order matters\r\n" +
                "-s source1;recursive;target1,target2,...;exception1,exception2,...;source2;...\r\n" +
                "-l logFile\r\n" +
                "-ls logFileSize\r\n" +
                "-mbf minimalBlockFileSize\r\n" +
                "-bs blockSize\r\n" +
                "-p parallelSynchronization\r\n\r\n" +
                "or -h for help\r\n\r\n" +
                "For example:\r\n" +
                "-q d:\test;true;d:\test2,c:\test; -l Logs.txt -ls 10 -mbfs 10 -bs 10 -p true"
                );

            // Ignore if there are no arguments
            if (args == null || args.Length == 0)
            {
                return false;
            }

            string[] newArguments = new string[args.Length];

            // Trim and make to lower case
            for (int i = 0; i < args.Length; i++)
            {
                newArguments[i] = args[i].ToLower().Trim();
            }

            bool validArguments;

            try
            {
                validArguments = CheckArguments(args);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error, in validating arguments:\r\n{0}", exception.Message);
                validArguments = false;
            }

            if (!validArguments)
            {
                Console.WriteLine();
                Console.WriteLine(helpMessage);
                Console.WriteLine("Press a key to continue without the usage of arguments");
                Console.ReadKey();
                return false;
            }

            return true;
        }

        private bool CheckArguments(string[] args)
        {
            // If first argument is help
            if (args[0] == "-h")
            {
                return false;
            }

            if (args.Length != 12)
            {
                Console.WriteLine("Error, must contain 6 parameters with given values");
                return false;
            }
            if (args[0] != "-s" ||
                args[2] != "-l" ||
                args[4] != "-ls" ||
                args[6] != "-mbf" ||
                args[8] != "-bs" ||
                args[10] != "-p")
            {
                Console.WriteLine("Error, wrong order");
                return false;
            }
            // Validate sources
            string[] sources = args[1].Split('|');
            int i = 0;

            foreach (var sourceRow in sources)
            {
                if (string.IsNullOrEmpty(sourceRow.Trim()))
                {
                    continue;
                }

                string[] sourceElements = sourceRow.Split(';');

                if (sourceElements.Length != 4)
                {
                    Console.WriteLine("Error, each source row must contain 4 values");
                    return false;
                }

                int j = 0;

                foreach (var sourceSection in sourceElements)
                {
                    if (j == 0)
                    {
                        DirectoryInfo directory;

                        if (!ValidationMethods.DirectoryInfoTryParse(sourceSection, out directory))
                        {
                            Console.WriteLine("Error, {0} is not a valid directory path", sourceSection);
                            return false;
                        }


                        bool recursive;
                        if (!bool.TryParse(sourceElements[1], out recursive))
                        {
                            Console.WriteLine("Error, {0} is not a valid recursive value", sourceSection);
                            return false;
                        }

                        string errorMessage = ApplicationManager.AddSource(directory.FullName, recursive);

                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            Console.WriteLine("Error, couldn't create source {0} because: \r\n{1}", sourceSection, errorMessage);
                            return false;
                        }

                    }
                    else if (j == 2)
                    {
                        if (string.IsNullOrEmpty(sourceSection.Trim()))
                        {
                            j++;
                            continue;
                        }

                        string[] targets = sourceSection.Split(',');

                        foreach (var target in targets)
                        {
                            DirectoryInfo directory;

                            if (!ValidationMethods.DirectoryInfoTryParse(target, out directory))
                            {
                                Console.WriteLine("Error, {0} is not a valid directory path", target);
                                return false;
                            }

                            string errorMessage = ApplicationManager.AddTarget(i, directory.FullName);
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                Console.WriteLine("Error, couldn't create target {0} because: \r\n{1}", target, errorMessage);
                                return false;
                            }
                        }
                    }
                    else if (j == 3)
                    {
                        if (string.IsNullOrEmpty(sourceSection.Trim()))
                        {
                            j++;
                            continue;
                        }

                        string[] exceptions = sourceSection.Split(',');

                        foreach (var exception in exceptions)
                        {
                            DirectoryInfo directory;

                            if (!ValidationMethods.DirectoryInfoTryParse(exception, out directory))
                            {
                                Console.WriteLine("Error, {0} is not a valid directory path", exception);
                                return false;
                            }

                            string errorMessage = ApplicationManager.AddException(i, directory.FullName);
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                Console.WriteLine("Error, couldn't create exception {0} because: \r\n{1}", exception, errorMessage);
                                return false;
                            }
                        }
                    }

                    j++;
                }

                i++;
            }

            // Validate logfile
            FileInfo loggingFile;

            if (!ValidationMethods.FileInfoTryParse(args[3], out loggingFile))
            {
                Console.WriteLine("Error, {0} is not a valid directory path", args[3]);
                return false;
            }

            ApplicationManager.Settings.LoggingFile = loggingFile;

            // Validate log file size
            long logFileSize;

            if (!long.TryParse(args[5], out logFileSize) || !ValidationMethods.IsValidLoggingFileSize(logFileSize))
            {
                Console.WriteLine("Error, {0} is not a valid logging file size", args[5]);
                return false;
            }

            ApplicationManager.Settings.MaxLoggingFileSize = logFileSize;

            // Validate minimal block file size
            long minimalBlockFileSize;

            if (!long.TryParse(args[7], out minimalBlockFileSize) || !ValidationMethods.IsValidFileSize(minimalBlockFileSize))
            {
                Console.WriteLine("Error, {0} is not a valid minimal block file size", args[7]);
                return false;
            }

            ApplicationManager.Settings.BlockCompareMinFileSize = minimalBlockFileSize;

            // Validate block size
            int blockSize;

            if (!int.TryParse(args[9], out blockSize) || !ValidationMethods.IsValidBlockSize(blockSize))
            {
                Console.WriteLine("Error, {0} is not a valid block size", args[9]);
                return false;
            }

            ApplicationManager.Settings.BlockCompareBlockSize = blockSize;

            // Validate parallel sync
            bool parallelSync;
            if (!bool.TryParse(args[11], out parallelSync))
            {
                Console.WriteLine("Error, {0} is not a valid parallel synchronization value", args[11]);
                return false;
            }

            ApplicationManager.Settings.ParallelSync = parallelSync;
            return true;
        }

        private void RefreshJobs(object sender, EventArgs args)
        {
            if (this.currentMenu == Menu.Jobs)
            {
                this.ChangeMenu();
            }
        }

        public void Start()
        {
            // Validate if the input files are okay.
            string errorMessage = ApplicationManager.ValidateData();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Console.WriteLine(errorMessage);
                Console.WriteLine("Change and restart application. You can also delete the *.save files to create a new structure. Program will close");
                Console.ReadLine();
            }
            else
            {
                ConsoleKey pressedKey;
                ChangeMenu(Menu.Sources);
                bool quit = false;

                do
                {
                    pressedKey = Console.ReadKey().Key;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(" ");
                    Console.SetCursorPosition(0, Console.CursorTop);

                    if (this.menus.ContainsKey(pressedKey.ToString()))
                    {
                        this.ChangeMenu(this.menus[pressedKey.ToString()]);
                    }

                    switch (pressedKey)
                    {
                        case ConsoleKey.A:
                            switch (currentMenu)
                            {
                                case Menu.Sources:
                                    AddSource();
                                    break;
                                case Menu.Targets:
                                    AddTarget();
                                    break;
                                case Menu.Exceptions:
                                    AddException();
                                    break;
                            }
                            break;
                        case ConsoleKey.D:
                            switch (currentMenu)
                            {
                                case Menu.Sources:
                                    DeleteSource();
                                    break;
                                case Menu.Targets:
                                    DeleteTarget();
                                    break;
                                case Menu.Exceptions:
                                    DeleteException();
                                    break;
                            }
                            break;
                        case ConsoleKey.Escape:
                            {
                                quit = CanEscape();
                                break;
                            }
                    }

                } while (!quit);

                ApplicationManager.SaveSettings();

                try
                {
                    if (this.loggingProcess != null && !this.loggingProcess.HasExited)
                    {
                        this.loggingProcess.Kill();
                        this.loggingProcess.Dispose();
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private bool CanEscape()
        {
            if (JobManager.HasFinished())
            {
                return true;
            }
            else
            {
                bool validAnswer;
                string input;
                Console.Write("Warning: Not all jobs completed, closing the program now leads to an asynchronous program state.\r\nAre you sure you want to close? y/n ");

                do
                {
                    validAnswer = false;
                    input = Console.ReadLine().Trim().ToLower();

                    if (input == "y" || input == "n")
                    {
                        validAnswer = true;
                    }
                    else
                    {
                        Console.Write("Error. Input must be \"y\" or \"n\" ");
                    }

                } while (!validAnswer);

                return input == "y" ? true : false;
            }
        }

        private void ChangeMenu(Menu? newMenu = null)
        {
            if (newMenu != null)
            {
                bool canChange = true;

                if (newMenu.Value == Menu.Targets)
                {
                    canChange = this.ChangeToTargets();
                }
                else if (newMenu.Value == Menu.Exceptions)
                {
                    canChange = this.ChangeToExceptions();
                }
                else if (newMenu.Value == Menu.Settings)
                {
                    this.PrintSettings();
                    canChange = false;
                }

                if (canChange)
                {
                    this.currentMenu = newMenu.Value;
                }
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            string separator = "_________________________";
            Console.WriteLine(separator);
            Console.WriteLine("Command Keys:");

            foreach (var menu in this.menus)
            {
                Console.WriteLine(menu.Key.ToString() + "  " + menu.Value);
            }

            Console.WriteLine("ESC Exit");

            if (currentMenu == Menu.Sources || currentMenu == Menu.Targets || currentMenu == Menu.Exceptions)
            {
                Console.WriteLine("A   Add");
                Console.WriteLine("D   Delete");
            }

            Console.WriteLine("Current Menu: {0} {1}",
                this.menus.First(p => p.Value.ToString() == this.currentMenu.ToString()).Key,
                this.currentMenu.ToString());

            if ((currentMenu == Menu.Targets || currentMenu == Menu.Exceptions) && this.currentSourceId.HasValue)
            {
                var currentSource = ApplicationManager.GetSources()[currentSourceId.Value];
                Console.WriteLine("{0} for source {1} {2}",
                    currentMenu.ToString(),
                    this.currentSourceId,
                    currentSource);
            }

            Console.WriteLine(separator);
            Console.ForegroundColor = ConsoleColor.Gray;

            switch (this.currentMenu)
            {
                case Menu.Sources:
                    this.PrintSources();
                    break;
                case Menu.Targets:
                    this.PrintTargets();
                    break;
                case Menu.Exceptions:
                    this.PrintExceptions();
                    break;
                case Menu.Logs:
                    this.PrintLogs();
                    break;
                case Menu.Jobs:
                    this.PrintJobs();
                    break;
                case Menu.Help:
                    this.ChangeMenu();
                    break;
            }
        }

        private void PrintSources()
        {
            var sources = ApplicationManager.GetSources();
            PrintListWithIndex(sources);
        }

        private void AddSource()
        {
            if (AddPath(PathOperation.Source))
            {
                this.ChangeMenu();
            }
            else
            {
                Console.WriteLine("No path entered");
            }
        }

        private void DeleteSource()
        {
            var sources = ApplicationManager.GetSources();
            if (sources == null || sources.Count == 0)
            {
                Console.WriteLine("Can't delete source because there are no sources.");
            }
            else
            {
                int id;
                if (GetIdInputFromCollection(out id, sources, IdOperation.DeleteSource))
                {
                    ApplicationManager.DeleteSource(id);
                    this.ChangeMenu(Menu.Sources);
                }
                else
                {
                    Console.WriteLine("No id entered.");
                }
            }
        }

        private bool GetIdInputFromCollection(out int id, List<string> collection, IdOperation operation)
        {
            bool isValid = false;
            id = 0;

            do
            {
                string information = string.Empty;

                switch (operation)
                {
                    case IdOperation.DeleteSource:
                    case IdOperation.DeleteTarget:
                    case IdOperation.DeleteException:
                        information = " to delete";
                        break;
                    case IdOperation.GetTargets:
                        information = " for targets";
                        break;
                    case IdOperation.GetExceptions:
                        information = " for sources";
                        break;
                }

                Console.Write("Enter id {0} (exit to cancel): ", information);
                string input = Console.ReadLine().Trim();

                if (input == "exit")
                {
                    return false;
                }

                bool isValidNumber = int.TryParse(input, out id);

                if (isValidNumber && id >= 0 && id < collection.Count())
                {
                    isValid = true;
                    string errorMessage = null;

                    switch (operation)
                    {
                        case IdOperation.DeleteException:
                            errorMessage = ApplicationManager.DeleteException(this.currentSourceId.Value, id);
                            break;
                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        isValid = false;
                        Console.WriteLine("Couldn't delete exception {0}:\r\n{1}", id, errorMessage);
                    }
                }
                else
                {
                    Console.WriteLine("Error. Enter a valid number between 0 and " + (collection.Count() - 1));
                }

            } while (!isValid);

            return true;
        }

        private bool ChangeToTargets()
        {
            var sources = ApplicationManager.GetSources();
            if (sources.Count() == 0)
            {
                Console.WriteLine("Can't open targets because there are no sources.");
                return false;
            }
            else
            {
                int sourceId;
                if (this.GetIdInputFromCollection(out sourceId, sources, IdOperation.GetTargets))
                {
                    this.currentSourceId = sourceId;
                    return true;
                }
                else
                {
                    Console.WriteLine("No id entered.");
                    return false;
                }
            }
        }

        private void PrintTargets()
        {
            this.PrintListWithIndex(ApplicationManager.GetTargets(this.currentSourceId.Value));
        }

        private void AddTarget()
        {
            if (AddPath(PathOperation.Target))
            {
                this.ChangeMenu();
            }
            else
            {
                Console.WriteLine("No path entered");
            }
        }

        private void DeleteTarget()
        {
            var targets = ApplicationManager.GetTargets(this.currentSourceId.Value);

            if (targets == null || targets.Count == 0)
            {
                Console.WriteLine("Can't delete target because there are no targets.");
            }
            else
            {
                int id;
                if (GetIdInputFromCollection(out id, targets, IdOperation.DeleteTarget))
                {
                    ApplicationManager.DeleteTarget(this.currentSourceId.Value, id);
                    this.ChangeMenu();
                }
                else
                {
                    Console.WriteLine("No id entered.");
                }
            }
        }

        private bool ChangeToExceptions()
        {
            var sources = ApplicationManager.GetSources();
            if (sources.Count() == 0)
            {
                Console.WriteLine("Can't open exceptions because there are no sources");
                return false;
            }
            else
            {
                int sourceId;
                if (this.GetIdInputFromCollection(out sourceId, sources, IdOperation.GetExceptions))
                {
                    this.currentSourceId = sourceId;
                    return true;
                }
                else
                {
                    Console.WriteLine("No id entered");
                    return false;
                }
            }
        }

        private void PrintExceptions()
        {
            this.PrintListWithIndex(ApplicationManager.GetExceptions(this.currentSourceId.Value));
        }

        private void AddException()
        {
            if (AddPath(PathOperation.Exception))
            {
                this.ChangeMenu();
            }
            else
            {
                Console.WriteLine("No path entered");
            }
        }

        private void DeleteException()
        {
            var exceptions = ApplicationManager.GetExceptions(this.currentSourceId.Value);

            if (exceptions == null || exceptions.Count == 0)
            {
                Console.WriteLine("Can't delete exception because there are no exceptions.");
            }
            else
            {
                int id;
                if (GetIdInputFromCollection(out id, exceptions, IdOperation.DeleteException))
                {
                    this.ChangeMenu();
                }
                else
                {
                    Console.WriteLine("No id entered.");
                }
            }
        }

        private void PrintJobs()
        {
            var jobs = ApplicationManager.GetJobs();
            foreach (var item in jobs)
            {
                Console.WriteLine(item);
            }
        }

        private void PrintLogs()
        {
            //var logs = ApplicationManager.GetLogs();
            //foreach (var log in logs)
            //{
            //    Console.WriteLine(log);
            //}
        }

        private void PrintSettings()
        {
            string blockCompareMinFileSize = Convert.ToString(ApplicationManager.Settings.BlockCompareMinFileSize);
            string blockCompareBlockSize = Convert.ToString(ApplicationManager.Settings.BlockCompareBlockSize);
            string parallelSync = Convert.ToString(ApplicationManager.Settings.ParallelSync);
            string loggingFilePath = ApplicationManager.Settings.LoggingFile.FullName;
            string loggingFileMaxSize = Convert.ToString(ApplicationManager.Settings.MaxLoggingFileSize);

            Console.WriteLine("The current settings are:");
            Console.WriteLine("Block compare minimal file size: " + blockCompareMinFileSize);
            Console.WriteLine("Block compare block size: " + blockCompareBlockSize);
            Console.WriteLine("Parallel synchronisation: " + parallelSync);
            Console.WriteLine("Logging file: " + loggingFilePath);
            Console.WriteLine("Logging file maximal file size: " + loggingFileMaxSize);

            // Get want to change value
            bool wantChange;
            GetInput<bool>(ValidationMethods.YesNoTryParse, out wantChange, "Want to change? y/n ", "Invalid input. Must be \"n\" or \"y\"");

            if (wantChange)
            {
                // Get block compare minimal file size value
                long newBlockCompareMinFileSize;

                if (this.GetInput(
                    long.TryParse,
                    out newBlockCompareMinFileSize,
                    string.Format("New block compare minimal file size (empty to keep {0}): ", blockCompareMinFileSize),
                    string.Format("Error, value must be a number between 0 and {0}", long.MaxValue),
                    false,
                    true,
                    ValidationMethods.IsValidFileSize))
                {
                    ApplicationManager.Settings.BlockCompareMinFileSize = newBlockCompareMinFileSize;
                }

                // Get block compare block size value
                int newBlockCompareBlockSize;

                if (this.GetInput(
                    int.TryParse,
                    out newBlockCompareBlockSize,
                    string.Format("New block compare block size (empty to keep {0}): ", blockCompareBlockSize),
                    string.Format("Error, value must be a number between 1 and {0}", Int32.MaxValue),
                    false,
                    true,
                    ValidationMethods.IsValidBlockSize))
                {
                    ApplicationManager.Settings.BlockCompareBlockSize = newBlockCompareBlockSize;
                }

                // Get parallel sync value
                bool newParallelSyncValue;

                if (this.GetInput(
                    bool.TryParse,
                    out newParallelSyncValue,
                    string.Format("New parallel synchronisation (empty to keep {0}): ", parallelSync),
                    "Error, value must be a \"true\" or \"false\"",
                    false,
                    true))
                {
                    ApplicationManager.Settings.ParallelSync = newParallelSyncValue;
                }

                // Get logging file path
                FileInfo newloggingFilePath;

                if (this.GetInput(
                    ValidationMethods.FileInfoTryParse,
                    out newloggingFilePath,
                    string.Format("New logging file (empty to keep {0}): ", loggingFilePath),
                    "Error, path must be a valid file",
                    false,
                    true))
                {
                    ApplicationManager.Settings.LoggingFile = newloggingFilePath;
                }

                // Get logging file size
                long newMaxLoggingFileSize;



                if (this.GetInput(
                    long.TryParse,
                    out newMaxLoggingFileSize,
                    string.Format("New logging file maximal file size (empty to keep {0}): ", loggingFileMaxSize),
                    string.Format("Error, value must be a number between 1 and {0}", long.MaxValue),
                    false,
                    true,
                    ValidationMethods.IsValidLoggingFileSize))
                {
                    ApplicationManager.Settings.MaxLoggingFileSize = newMaxLoggingFileSize;
                }
            }
        }

        delegate bool TryParse<T>(string str, out T value);

        delegate bool IsValid<T>(T value);

        private bool GetInput<T>(TryParse<T> parseFunction, out T value, string inputMessage, string errorMessage, bool exitAllowed = false, bool emptyAllowed = false, IsValid<T> isValid = null)
        {
            // http://stackoverflow.com/questions/10574504/how-to-use-t-tryparse-in-a-generic-method-while-t-is-either-double-or-int
            value = default(T);

            bool canParse;
            do
            {
                Console.Write(inputMessage);
                string input = Console.ReadLine().Trim().ToLower();

                if ((exitAllowed && input == "exit") ||
                    (emptyAllowed && input == string.Empty))
                {
                    return false;
                }

                canParse = parseFunction(input, out value);

                if (canParse && isValid != null)
                {
                    if (!isValid(value))
                    {
                        canParse = false;
                    }
                }

                if (!canParse)
                {
                    Console.WriteLine(errorMessage);
                }

            } while (!canParse);

            return true;
        }

        private void PrintListWithIndex(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                Console.WriteLine("No entries");
            }
            else
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    Console.WriteLine(i + " " + values[i]);
                }
            }
        }

        private bool AddPath(PathOperation operation)
        {
            string information = string.Empty;

            switch (operation)
            {
                case PathOperation.Source:
                    information = "sources";
                    break;
                case PathOperation.Target:
                    information = "targets";
                    break;
                case PathOperation.Exception:
                    information = "exceptions";
                    break;
                default:
                    break;
            }

            DirectoryInfo newDirectory;

            IsValid<DirectoryInfo> isValid = delegate (DirectoryInfo value)
            {
                string errorMessage = string.Empty;

                switch (operation)
                {
                    case PathOperation.Source:
                        errorMessage = ApplicationManager.AddSource(value.FullName);
                        break;
                    case PathOperation.Target:
                        errorMessage = ApplicationManager.AddTarget(this.currentSourceId.Value, value.FullName);
                        break;
                    case PathOperation.Exception:
                        errorMessage = ApplicationManager.AddException(this.currentSourceId.Value, value.FullName);
                        break;
                }

                if (errorMessage != null)
                {
                    Console.WriteLine("Couldn't add {0}:\r\n{1}", operation.ToString(), errorMessage);
                    return false;
                }
                else
                {
                    return true;
                }
            };

            if (this.GetInput(
                ValidationMethods.DirectoryInfoTryParse,
                out newDirectory,
                string.Format("Enter path for new {0} (\"exit\" to cancel): ", operation.ToString()),
                "Error, not a valid path",
                true,
                false,
                isValid))
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
