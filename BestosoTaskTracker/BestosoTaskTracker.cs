using System;

namespace BestosoTaskTracker
{
    class BestosoTaskTracker
    {
        public const int CONTEXT_MAIN_MENU = 0;
        public const int CONTEXT_VIEW_TASKS = 1;
        public const int CONTEXT_SETUP_DB = 2;
        
        static SQLController sqlHandler = new SQLController();
        static TaskManager taskManager = new TaskManager();

        public static string GetMaskedInput(string sectionTitle=null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if(sectionTitle != null)
                Console.WriteLine($"--= {sectionTitle} =--");
            
            string input = string.Empty;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(":)> ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && input.Length > 0)
                {
                    Console.Write("\b \b");
                    input = input[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    input += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            Console.ForegroundColor = ConsoleColor.Yellow;
            return input;
        }

        public static string GetUserInput(string sectionTitle, string selectedTaskName=null, bool allowExit=true)
        {
            string ret = null;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"--= {sectionTitle} =--");
            
            if(allowExit)
                Console.WriteLine("\tType 'exit' to cancel");
            
            if (selectedTaskName != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{selectedTaskName} ");
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(":)> ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            ret = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            
            if (ret == null)
                return null;
            if (ret.Contains("exit") && allowExit)
                return null;

            return ret;
        }
        
        public static int GetUserInputInt(string sectionTitle, string selectedTaskName=null)
        {
            int ret = -1;
            string grabber = null;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"--= {sectionTitle} =--");
            Console.WriteLine("\tType 'exit' to cancel");
            if (selectedTaskName != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{selectedTaskName} ");
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(":)> ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            grabber = Console.ReadLine();
            if (grabber == "exit")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                return -1;
            }
            try
            {
                if (grabber == null)
                    throw new ArgumentNullException(nameof(ret), "Received a null value from the console.");
                
                ret = Int32.Parse(grabber);
                
            }
            catch (FormatException)
            {
                Console.WriteLine($"Unable to parse '{ret}'");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;

            return ret;
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[-] {message}");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] {message}");
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        public static void PrintStringList(string[] list, string title = null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (title != null)
            {
                Console.WriteLine(title);
            }

            for (int i=0; i<list.Length; i++) {
                Console.WriteLine($"{list[i]}");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        static string MainMenu()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--= Main Menu =--");
            Console.WriteLine("[0] View Tasks");
            Console.WriteLine("[1] Set Up Database");
            Console.WriteLine("[2] Exit");
            Console.Write(":)> ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            string ret = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            return ret;
        }
        
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            bool running = true;
            int context = CONTEXT_SETUP_DB;
            
            while (running)
            {
                switch (context)
                {
                    case CONTEXT_MAIN_MENU:
                    {
                        string command = MainMenu();
                        if (command == "2")
                        {
                            sqlHandler?.SqlConnection?.Close();
                            running = false;
                        }else if (command == "0")
                        {
                            context = CONTEXT_VIEW_TASKS;
                            Console.Clear();
                        }else if (command == "1")
                        {
                            context = CONTEXT_SETUP_DB;
                            Console.Clear();
                        }
                        else
                        {
                            Console.Clear();
                            Console.WriteLine("[INVALID MENU OPTION]");
                        }
                        break;
                    }
                    case CONTEXT_SETUP_DB:
                    {
                        context = CONTEXT_MAIN_MENU;
                        sqlHandler.InitTaskDataBase();
                        break;
                    }
                    case CONTEXT_VIEW_TASKS:
                    {
                        taskManager.SqlHandler = sqlHandler;
                        context = taskManager.TaskManagerMainMenu();
                        break;
                    }
                }
            }
        }
    }
}