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
            F3,     // Sources
            F4,     // Targets
            F5,     // Exceptions
            F6,     // Jobs
            F7,     // Logs
            F8,     // Settings
            F9      // Help
        }

        private Dictionary<Menu, string> menus;

        private Menu currentMenu;

        int currentSourceId;

        public PresentationManager()
        {
            this.menus = new Dictionary<Menu, string>();
            this.menus.Add(Menu.F3, "Sources");
            this.menus.Add(Menu.F4, "Tragets");
            this.menus.Add(Menu.F5, "Exceptions");
            this.menus.Add(Menu.F6, "Jobs");
            this.menus.Add(Menu.F7, "Logs");
            this.menus.Add(Menu.F8, "Settings");
            this.menus.Add(Menu.F9, "Help");

            this.Logs = new ConcurrentBag<string>();
            this.applicationManager = new ApplicationManager();
            this.currentSourceId = 0;
        }

        public void Start()
        {
            ConsoleKey pressedKey;
            ChangeMenu(Menu.F3);
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
                        this.ChangeMenu(Menu.F3);
                        PrintSources();
                        break;
                    case ConsoleKey.F4:
                        CanPrintTargets();
                        break;
                    case ConsoleKey.F5:
                        CanPrintExceptions();
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
                            case Menu.F3:
                                AddSource();
                                break;
                            case Menu.F4:
                                break;
                            case Menu.F5:
                                break;
                        }
                        break;
                    case ConsoleKey.D:
                        switch (currentMenu)
                        {
                            case Menu.F3:
                                break;
                            case Menu.F4:
                                break;
                            case Menu.F5:
                                break;
                        }
                        break;
                }

            } while (pressedKey != ConsoleKey.Escape);

            applicationManager.SaveSettings();
        }

        private void ChangeMenu(Menu newMenu)
        {
            this.currentMenu = newMenu;

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            string separator = "_________________________";
            Console.WriteLine(separator);
            Console.WriteLine("Command Keys:");

            foreach (var menu in this.menus)
            {
                Console.WriteLine(menu.Key.ToString() + " " + menu.Value);
            }

            if (currentMenu == Menu.F3 || currentMenu == Menu.F4 || currentMenu == Menu.F5)
            {
                Console.WriteLine("A  Add");
                Console.WriteLine("D  Delete");
            }

            Console.WriteLine("Current Menu: " + this.currentMenu.ToString() + " " + this.menus[this.currentMenu]);
            Console.WriteLine(separator);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void PrintSources()
        {
            var sources = applicationManager.GetSources();
            foreach (var source in sources)
            {
                Console.WriteLine(source);
            }
        }

        private void AddSource()
        {
            string newDirectoryPath;
            var sources = applicationManager.GetSources();

            if (GetPathInput(out newDirectoryPath, sources))
            {
                applicationManager.AddSource(newDirectoryPath);
                PrintSources();
            }
            else
            {
                Console.WriteLine("No path entered");
            }
        }

        private void DeleteSource()
        {

        }

        private bool GetIdInputFromCollection(out int id, List<string> collection)
        {
            bool isValid = false;
            id = 0;

            do
            {
                Console.Write("Enter source id (exit to cancel): ");
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

        private void CanPrintTargets()
        {
            var sources = applicationManager.GetSources();
            if (sources.Count() == 0)
            {
                Console.WriteLine("Can't open targets because there are no sources.");
                this.ChangeMenu(Menu.F3);
            }
            else
            {
                int sourceId;
                if (this.GetIdInputFromCollection(out sourceId, sources))
                {
                    Console.WriteLine("Targets for source: " + sourceId + " " + sources[sourceId]);
                    this.PrintListWithIndex(applicationManager.GetTargets(sourceId));
                    this.ChangeMenu(Menu.F4);
                }
                else
                {
                    Console.WriteLine("No id entered");
                    this.ChangeMenu(Menu.F3);
                }
            }
        }

        private void AddTarget()
        {

        }

        private void DeleteTarget()
        {

        }

        private void CanPrintExceptions()
        {
            var sources = applicationManager.GetSources();
            if (sources.Count() == 0)
            {
                Console.WriteLine("Can't open exceptions because there are no sources");
                this.ChangeMenu(Menu.F3);
            }
            else
            {
                int sourceId;
                if (this.GetIdInputFromCollection(out sourceId, sources))
                {
                    Console.WriteLine("Exceptions for source: " + sourceId + " " + sources[sourceId]);
                    this.PrintListWithIndex(applicationManager.GetExceptions(sourceId));
                    this.ChangeMenu(Menu.F5);
                }
                else
                {
                    Console.WriteLine("No id entered");
                    this.ChangeMenu(Menu.F3);
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
            for (int i = 0; i < values.Count(); i++)
            {
                Console.WriteLine(i + " " + values[i]);
            }
        }

        private bool GetPathInput(out string resultPath, List<string> existingPaths)
        {
            resultPath = string.Empty;
            string path;
            bool isValid = false;

            do
            {
                Console.Write("Enter path: ");
                path = Console.ReadLine().Replace('/', '\\').Trim().TrimEnd('\\').ToLower();

                if (path == "exit")
                {
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Error. Not a valid path.");
                }
                else if (existingPaths.Any(p => Path.GetFullPath(p) == Path.GetFullPath(path)))
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
