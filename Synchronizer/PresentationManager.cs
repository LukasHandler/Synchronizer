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
        private ApplicationManager applicationManager;

        private ConcurrentBag<string> Logs;

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
            Delete,
            Targets,
            Exceptions
        }

        private enum PathOperation
        {
            Source,
            Target,
            Exception
        }

        private Dictionary<string, Menu> menus;

        private Menu currentMenu;

        private int currentSourceId;

        public PresentationManager()
        {
            this.menus = new Dictionary<string, Menu>();
            this.menus.Add("F3", Menu.Sources);
            this.menus.Add("F4", Menu.Targets);
            this.menus.Add("F5", Menu.Exceptions);
            this.menus.Add("F6", Menu.Jobs);
            this.menus.Add("F7", Menu.Logs);
            this.menus.Add("F8", Menu.Help);
            this.menus.Add("F9", Menu.Settings);

            this.Logs = new ConcurrentBag<string>();
            this.applicationManager = new ApplicationManager();
            this.currentSourceId = 0;
        }

        public void Start()
        {
            ConsoleKey pressedKey;
            ChangeMenu(Menu.Sources);
            PrintSources();

            do
            {
                pressedKey = Console.ReadKey().Key;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(" ");
                Console.SetCursorPosition(0, Console.CursorTop);

                switch (pressedKey)
                {
                    case ConsoleKey.F3:
                        this.ChangeMenu(Menu.Sources);
                        PrintSources();
                        break;
                    case ConsoleKey.F4:
                        PrintTargets();
                        break;
                    case ConsoleKey.F5:
                        PrintExceptions();
                        break;
                    case ConsoleKey.F6:
                        PrintJobs();
                        break;
                    case ConsoleKey.F7:
                        PrintLogs();
                        break;
                    case ConsoleKey.F8:
                        PrintSettings();
                        break;
                    case ConsoleKey.F9:
                        ChangeMenu(this.currentMenu);
                        break;
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
                }

            } while (pressedKey != ConsoleKey.Escape);

            applicationManager.SaveSettings();
        }

        private void ChangeMenu(Menu? newMenu = null)
        {
            if (newMenu != null)
            {
                this.currentMenu = newMenu.Value;
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            string separator = "_________________________";
            Console.WriteLine(separator);
            Console.WriteLine("Command Keys:");

            foreach (var menu in this.menus)
            {
                Console.WriteLine(menu.Key.ToString() + " " + menu.Value);
            }

            if (currentMenu == Menu.Sources || currentMenu == Menu.Targets || currentMenu == Menu.Exceptions)
            {
                Console.WriteLine("A  Add");
                Console.WriteLine("D  Delete");
            }

            Console.WriteLine("Current Menu: {0} {1}",
                this.menus.First(p => p.Value.ToString() == this.currentMenu.ToString()).Key,
                this.currentMenu.ToString());

            if (currentMenu == Menu.Targets || currentMenu == Menu.Exceptions)
            {
                var currentSource = applicationManager.GetSources()[currentSourceId];
                Console.WriteLine("{0} for source {1}",
                    currentMenu.ToString(),
                    currentSource);
            }

            Console.WriteLine(separator);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void PrintSources()
        {
            var sources = applicationManager.GetSources();
            PrintListWithIndex(sources);
        }

        private void AddSource()
        {
            string newDirectoryPath;
            var sources = applicationManager.GetSources();

            if (GetPathInput(out newDirectoryPath, sources, PathOperation.Source))
            {
                applicationManager.AddSource(newDirectoryPath);
                this.ChangeMenu();
                PrintSources();
            }
            else
            {
                Console.WriteLine("No path entered");
            }
        }

        private void DeleteSource()
        {
            var sources = applicationManager.GetSources();
            if (sources == null || sources.Count == 0)
            {
                Console.WriteLine("Can't delete source because there are no sources.");
            }
            else
            {
                int id;
                if (GetIdInputFromCollection(out id, sources, IdOperation.Delete))
                {
                    applicationManager.DeleteSource(id);
                    this.ChangeMenu(Menu.Sources);
                    this.PrintSources();
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
                    case IdOperation.Delete:
                        information = " to delete";
                        break;
                    case IdOperation.Targets:
                        information = " for targets";
                        break;
                    case IdOperation.Exceptions:
                        information = " for sources";
                        break;
                }

                Console.Write("Enter source id {0} (exit to cancel): ", information);
                string input = Console.ReadLine().Trim();

                if (input == "exit")
                {
                    return false;
                }

                bool isValidNumber = int.TryParse(input, out id);

                if (isValidNumber && id >= 0 && id < collection.Count())
                {
                    isValid = true;
                }
                else
                {
                    Console.WriteLine("Error. Enter a valid number between 0 and " + (collection.Count() - 1));
                }

            } while (!isValid);

            return true;
        }

        private void PrintTargets()
        {
            var sources = applicationManager.GetSources();
            if (sources.Count() == 0)
            {
                Console.WriteLine("Can't open targets because there are no sources.");
                this.ChangeMenu(Menu.Sources);
            }
            else
            {
                int sourceId;
                if (this.GetIdInputFromCollection(out sourceId, sources, IdOperation.Targets))
                {
                    this.currentSourceId = sourceId;
                    this.ChangeMenu(Menu.Targets);
                    this.PrintListWithIndex(applicationManager.GetTargets(sourceId));
                }
                else
                {
                    Console.WriteLine("No id entered.");
                    this.ChangeMenu(Menu.Sources);
                }
            }
        }

        private void AddTarget()
        {
            string newDirectoryPath = null;

            if (GetPathInput(out newDirectoryPath, null, PathOperation.Target))
            {
                applicationManager.AddTarget(currentSourceId, newDirectoryPath);
                this.ChangeMenu();
                this.PrintListWithIndex(applicationManager.GetTargets(this.currentSourceId));
            }
            else
            {
                Console.WriteLine("No path entered");
            }
        }

        private void DeleteTarget()
        {

        }

        private void PrintExceptions()
        {
            var sources = applicationManager.GetSources();
            if (sources.Count() == 0)
            {
                Console.WriteLine("Can't open exceptions because there are no sources");
                this.ChangeMenu(Menu.Sources);
            }
            else
            {
                int sourceId;
                if (this.GetIdInputFromCollection(out sourceId, sources, IdOperation.Exceptions))
                {
                    this.currentSourceId = sourceId;
                    this.ChangeMenu(Menu.Exceptions);
                    this.PrintListWithIndex(applicationManager.GetExceptions(sourceId));
                }
                else
                {
                    Console.WriteLine("No id entered");
                    this.ChangeMenu(Menu.Sources);
                }

            }
        }

        private void AddException()
        {

        }

        private void DeleteException()
        {

        }

        private void PrintJobs()
        {

        }

        private void PrintLogs()
        {
            var logs = applicationManager.GetLogs();
            foreach (var log in logs)
            {
                Console.WriteLine(log);
            }
        }

        private void PrintSettings()
        {

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

        private bool GetPathInput(out string resultPath, List<string> existingPaths, PathOperation operation)
        {
            resultPath = string.Empty;
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
                path = Console.ReadLine().Replace('/', '\\').Trim().TrimEnd('\\').ToLower();

                if (path == "exit")
                {
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Error. Not a valid path.");
                }
                else if (existingPaths != null && existingPaths.Any(p => Path.GetFullPath(p) == Path.GetFullPath(path)))
                {
                    Console.WriteLine("Error. This path already exists");
                }
                else
                {
                    isValid = true;
                }

            } while (!isValid);

            resultPath = path;
            return true;
        }
    }
}
