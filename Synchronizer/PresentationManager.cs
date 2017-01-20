using Synchronizer.ApplicationLogic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Synchronizer.Shared.EventArguments;
using System.Collections;
using System.IO;

namespace Synchronizer.PresentationLogic
{
    public class PresentationManager
    {
        private ConcurrentBag<string> Logs;

        private ConcurrentBag<string> JobLogs;

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

        public PresentationManager()
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

            JobManager.OnLog += NewJobLog;

            this.currentSourceId = 0;
        }

        private void NewJobLog(object sender, LogEventArguments e)
        {
            this.JobLogs.Add(DateTime.Now.ToString("ddMMyyyy:HHmmss") + e.LogMessage);
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
                else  if (newMenu.Value == Menu.Settings)
                {
                    this.PrintSettings();
                    canChange = false;
                }

                if (canChange)
                {
                    this.currentMenu = newMenu.Value;
                }
            }

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
            var logs = ApplicationManager.GetLogs();
            foreach (var log in logs)
            {
                Console.WriteLine(log);
            }
        }

        private void PrintSettings()
        {
            string blockCompareMinFileSize = Convert.ToString(ApplicationManager.GetBlockCompareMinFileSize());
            string blockCompareBlockSize = Convert.ToString(ApplicationManager.GetBlockCompareBlockSize());
            string parallelSync = Convert.ToString(ApplicationManager.GetParallelSync());

            Console.WriteLine("The current settings are:");
            Console.WriteLine("Block compare minimal file size: " + blockCompareMinFileSize);
            Console.WriteLine("Block compare block size: " + blockCompareBlockSize);
            Console.WriteLine("Parallel synchronisation: " + parallelSync);

            string input;
            bool goodInput;

            // Get want to change value
            do
            {
                Console.Write("Want to change? y/n ");
                input = Console.ReadLine().Trim().ToLower();
                if (input != "y" && input != "n")
                {
                    goodInput = false;
                    Console.WriteLine("Invalid input. Must be \"n\" or \"y\"");
                }
                else
                {
                    goodInput = true;
                }

            } while (!goodInput);

            long newValue = 0;

            if (input == "y")
            {
                // Get block compare minimal file size value
                do
                {
                    Console.Write("New block compare minimal file size (empty to keep {0}): ", blockCompareMinFileSize);
                    input = Console.ReadLine().Trim();

                    if (input == string.Empty)
                    {
                        goodInput = true;
                    }
                    else if (Int64.TryParse(input, out newValue) && newValue > 0)
                    {
                        ApplicationManager.SetBlockCompareMinFileSize(newValue);
                        goodInput = true;
                    }
                    else
                    {
                        Console.WriteLine("Error, value must be a number between 0 and " + Int64.MaxValue);
                        goodInput = false;
                    }
                } while (!goodInput);

                // Get block compare block size value
                int newBlockCompareSize;
                do
                {
                    Console.Write("New block compare block size (empty to keep {0}): ", blockCompareBlockSize);
                    input = Console.ReadLine().Trim();

                    if (input == string.Empty)
                    {
                        goodInput = true;
                    }
                    else if (Int32.TryParse(input, out newBlockCompareSize))
                    {
                        // Value 0 doesn't make sense for block size comparison value.
                        if (newValue == 0)
                        {
                            goodInput = false;
                        }
                        else
                        {
                            ApplicationManager.SetBlockCompareBlockSize(newBlockCompareSize);
                            goodInput = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error, value must be a number between 1 and " + Int32.MaxValue);
                        goodInput = false;
                    }

                } while (!goodInput);

                // Get parallel sync value
                bool newParallelSyncValue = false;
                do
                {
                    Console.Write("New block compare block size (empty to keep {0}): ", parallelSync);
                    input = Console.ReadLine().Trim();

                    if (input == string.Empty)
                    {
                        goodInput = true;
                    }
                    else if (Boolean.TryParse(input, out newParallelSyncValue))
                    {
                        ApplicationManager.SetParallelSync(newParallelSyncValue);
                    }
                    else
                    {
                        Console.WriteLine("Error, value must be a \"true\" or \"false\"");
                        goodInput = false;
                    }

                } while (!goodInput);
            }
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
            string path;
            bool isValid = false;

            do
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

                Console.Write("Enter path for new {0} (\"exit\" to cancel): ", information);
                string input = Console.ReadLine();

                if (input == "exit")
                {
                    return false;
                }

                path = PathHelper.ChangePathToDefaultPath(input);

                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Error. Not a valid path.");
                }
                else
                {
                    string errorMessage = null;

                    switch (operation)
                    {
                        case PathOperation.Source:
                            errorMessage = ApplicationManager.AddSource(path);
                            break;
                        case PathOperation.Target:
                            errorMessage = ApplicationManager.AddTarget(this.currentSourceId.Value, path);
                            break;
                        case PathOperation.Exception:
                            errorMessage = ApplicationManager.AddException(this.currentSourceId.Value, path);
                            break;
                    }

                    if (errorMessage != null)
                    {
                        isValid = false;
                        Console.WriteLine("Couldn't add {0}:\r\n{1}", operation.ToString(), errorMessage);
                    }
                    else
                    {
                        isValid = true;
                    }
                }
            } while (!isValid);

            return true;
        }
    }
}
