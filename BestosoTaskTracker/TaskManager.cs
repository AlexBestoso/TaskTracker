using System;
using System.Collections.Generic;
using System.Linq;

namespace BestosoTaskTracker
{
    public class TaskManager
    {
        #region PrivateVars
        private SQLController sqlHandler;

        private string[] groupCommands = new string[]
        {
            "show",
            "create",
            "add",
            "remove",
            "rmgroup",
            "tasks",
            "select",
            "deselect",
        };

        private string[] taskCommands = new string[]
        {
            "add",
            "show",
            "completed",
            "offloaded",
            "done",
            "decomplete",
            "reload",
            "current",
            "nogroup",
            "select",
            "deselect",
            "details",
            "offload",
        };

        private string[] generalCommands = new string[]
        {
            "help",
            "exit",
            "update status",
            "clearcompletedtasks",
            "update priority",
            "update name",
            "clear",
            "task",
            "group",
        };
        
        private string taskName;
        private bool taskCompleted;
        private string taskStatus;
        private int taskPriority;
        #endregion

        #region PublicVars
        public SQLController SqlHandler
        {
            get { return sqlHandler; }
            set { sqlHandler = value; }
        }
        public string TaskName
        {
            get { return taskName; }
            set { taskName = value; }
        }
        public bool TaskCompleted
        {
            get => taskCompleted;
            set => taskCompleted = value;
        }
        public string TaskStatus
        {
            get => taskStatus;
            set => taskStatus = value;
        }
        public int TaskPriority
        {
            get => taskPriority;
            set => taskPriority = value;
        }
        

        #endregion

        #region PublicMethods

        public string[] TaskManagerGetUserInput()
        {
            List<string> ret = new List<string>();
            string cmd;
            Console.WriteLine("--= Task Manager =--");
            Console.WriteLine("Type 'help' for a list of commands");
            if (sqlHandler.SelectedId > 0 || sqlHandler.SelectedGroupId > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                string SelectionTracker = "(";
                if (sqlHandler.SelectedGroupId > 0)
                {
                    SelectionTracker = $"{SelectionTracker}GroupID: {sqlHandler.SelectedGroupId}";
                }

                if (sqlHandler.SelectedId > 0)
                {
                    if (SelectionTracker.Contains("GroupID"))
                        SelectionTracker = $"{SelectionTracker} | ";
                    SelectionTracker = $"{SelectionTracker}TaskID: {sqlHandler.SelectedId}";
                }

                SelectionTracker = $"{SelectionTracker})";

                Console.Write(SelectionTracker);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" :)> ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(":)> ");
                
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            cmd = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Yellow;

            return cmd.Split(" ", StringSplitOptions.None);
        }

        public int TaskManagerMainMenu()
        {
            int ret = BestosoTaskTracker.CONTEXT_VIEW_TASKS;
            string cmd = null;
            string[] args = TaskManagerGetUserInput();
            if (args.Length <= 0)
            {
                cmd = "help";
            }
            else
            {
                cmd = args[0];
            }

            switch (cmd)
            {
                case "help":
                {
                    Console.Clear();
                    Console.WriteLine("--= General Commands =--");
                    for (int i=0; i<generalCommands.Length; i++) {
                        Console.WriteLine($"{generalCommands[i]}");
                    }

                    Console.WriteLine("\n--= Task Commands =--");
                    for (int i=0; i<taskCommands.Length; i++) {
                        Console.WriteLine($"{taskCommands[i]}");
                    }

                    Console.WriteLine("\n--= Group Commands =--");
                    for (int i=0; i<groupCommands.Length; i++) {
                        Console.WriteLine($"{groupCommands[i]}");
                    }
                    break;
                }
                case "clear":
                {
                    Console.Clear();
                    break;
                }
                case "exit":
                {
                    ret = BestosoTaskTracker.CONTEXT_MAIN_MENU;
                    break;
                }
                case "update":
                {
                    string updateCategory = "";
                    if (args.Length == 1)
                    {
                        updateCategory = BestosoTaskTracker.GetUserInput("What would you like to update? (name, priority, status)?");
                        if (string.IsNullOrEmpty(updateCategory))
                        {
                            BestosoTaskTracker.PrintError("Failed To Update Task.");
                            break;
                        }
                    }
                    else if(args.Length >= 2)
                    {
                        updateCategory = args[1];
                    }

                    switch (updateCategory)
                    {
                        case "name":
                        {
                            if (sqlHandler.ExecuteUpdateTaskName())
                            {
                                BestosoTaskTracker.PrintError("Failed to update task name");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Successfully updated task name!");
                            }
                            break;
                        }
                        case "priority":
                        {
                            if (sqlHandler.SetTaskPriority())
                            {
                                BestosoTaskTracker.PrintError("Failed to update priority");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Successfully updated priority");
                            }
                            break;
                        }
                        case "status":
                        {
                            if (sqlHandler.SetTaskStatus())
                            {
                                BestosoTaskTracker.PrintError("Failed to update status");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Status Update Successful");
                            }
                            break;
                        }
                        default:
                        {
                            BestosoTaskTracker.PrintError("Invalid argument.");
                            break;
                        }
                    }
                    break;
                }
                case "task":
                {
                    string showCategory = "";
                    if (args.Length == 1)
                    {
                        BestosoTaskTracker.PrintStringList(taskCommands, "--= Available Task Commands =--");
                        break;
                    }
                    
                    if(args.Length >= 2)
                    {
                        showCategory = args[1];
                    }

                    switch (showCategory)
                    {
                        case "nogroup":
                        {
                            if (SqlHandler.GetTasksWithNoGroups())
                            {
                                BestosoTaskTracker.PrintError("Failed to fetch groupless tasks");
                            }

                            break;
                        }
                        case "reload":
                        {
                            int priority = -1;
                            if (args.Length >= 3)
                            {
                                if (!Int32.TryParse(args[2], out priority))
                                    priority = -1;
                            }

                            if (sqlHandler.ReloadOffloadedTasks(priority))
                            {   
                                BestosoTaskTracker.PrintError("Failed To Reload Offloaded Tasks");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Reloaded Offloaded Tasks!");
                            }

                            break;
                        }
                        case "current":
                        {
                            int id = sqlHandler.SelectedId <= 0 ? -1 : sqlHandler.SelectedId;
                            

                            if (args.Length >= 3)
                            {
                                if (!Int32.TryParse(args[2], out id))
                                    id = sqlHandler.SelectedId <= 0 ? -1 : sqlHandler.SelectedId;
                            }

                            if (sqlHandler.MarkTaskCurrent(id))
                            {
                                BestosoTaskTracker.PrintError("Failed to mark task as current.");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Task has been marked as current.");
                            }
                            break;
                        }
                        case "done":
                        {
                            if (sqlHandler.SetTaskAsComplete())
                            {
                                BestosoTaskTracker.PrintError("Failed to mark task as complete");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Task marked as completed!");
                                sqlHandler.SelectedId = 0;
                            }

                            break;
                        }
                        case "show":
                        {
                            if (sqlHandler.GetOpenTasks())
                            {
                                BestosoTaskTracker.PrintError("Failed to get opened tasks");
                            }

                            break;
                        }
                        case "completed":
                        {
                            if (sqlHandler.GetClosedTasks())
                            {
                                BestosoTaskTracker.PrintError("Failed to get closed tasks");
                            }

                            break;
                        }
                        case "offloaded":
                        {
                            if (sqlHandler.GetOffloadedTasks())
                            {
                                BestosoTaskTracker.PrintError("Failed to get offloaded tasks");
                            }
                            break;
                        }
                        case "offload":
                        {
                            if (sqlHandler.OffloadTask())
                            {
                                Console.WriteLine($"Failed to offload task");
                            }
                            else
                            {
                                Console.WriteLine("Offload successful");
                            }

                            break;
                        }
                        case "add":
                        {
                            if (sqlHandler.ExecuteInsertTask())
                            {
                                BestosoTaskTracker.PrintError("Failed To Add New Task");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Successfully Added New Task!");
                            }
                            break;
                        }
                        case "decomplete":
                        {
                            if (sqlHandler.SetTaskAsUncomplete())
                            {
                                BestosoTaskTracker.PrintError("Failed to mark task as active");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Task successfully marked as active");
                            }
                            break;
                        }
                        case "select":
                        {
                            sqlHandler.SelectedId = -1;
                            if (args.Length >= 3)
                            {
                                int id = -1;
                                if (!Int32.TryParse(args[2], out id))
                                {
                                    sqlHandler.SelectedId = BestosoTaskTracker.GetUserInputInt("Select Task ID");
                                }
                                else
                                {
                                    sqlHandler.SelectedId = id;
                                }
                            }
                            else
                            {
                                sqlHandler.SelectedId = BestosoTaskTracker.GetUserInputInt("Select Task ID");
                            }
                            break;
                        }
                        case "deselect":
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[+] Deselected {sqlHandler.SelectedId}");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            sqlHandler.SelectedId = 0;
                            break;
                        }
                        case "details":
                        {
                            if (sqlHandler.GetTaskDetails())
                            {
                                BestosoTaskTracker.PrintError("Failed to get details");
                            }
                            break;
                        }
                        default:
                        {
                            BestosoTaskTracker.PrintError("Invalid Input, Possible Commands Are : ");
                            BestosoTaskTracker.PrintStringList(taskCommands, null);
                            break;
                        }
                    }

                    break;
                }
                case "group":
                {
                    string showCategory = "";
                    if (args.Length == 1)
                    {
                        BestosoTaskTracker.PrintStringList(groupCommands, "--= Available Group Commands =--");
                        break;
                    }
                    
                    if(args.Length >= 2)
                    {
                        showCategory = args[1];
                    }

                    switch (showCategory)
                    {
                        case "create":
                        {
                            string groupName = null;
                            string groupDescription = null;

                            if (args.Length >= 3)
                            {
                                groupName = args[2];
                            }

                            if (args.Length >= 4)
                            {
                                groupDescription = args[3];
                            }

                            if (sqlHandler.CreateNewBestosoGroup(groupName, groupDescription))
                            {
                                BestosoTaskTracker.PrintError("Failed To Create A New Group!");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("New Group Successfully Added!");
                            }

                            break;
                        }
                        case "add":
                        {
                            int taskId = -1;
                            if (args.Length >= 3)
                            {
                                if (!Int32.TryParse(args[2], out taskId))
                                    taskId = -1;
                            }else if (SqlHandler.SelectedId > 0)
                            {
                                taskId = SqlHandler.SelectedId;
                            }

                            int groupId = -1;
                            if (args.Length >= 4)
                            {
                                if (!Int32.TryParse(args[3], out groupId))
                                    groupId = -1;
                            }else if (SqlHandler.SelectedGroupId > -1)
                            {
                                groupId = SqlHandler.SelectedGroupId;
                            }


                            if (SqlHandler.AddTaskToGroup(taskId, groupId))
                            {
                                BestosoTaskTracker.PrintError("Failed to add task to a group!");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Task added successfully!");
                            }

                            break;
                        }
                        case "show":
                        {
                            if (SqlHandler.ShowBestosoGroups())
                            {
                                BestosoTaskTracker.PrintError("Failed to get groups.");
                            }

                            break;
                        }
                        case "remove":
                        {
                            int groupId = -1;
                            int taskId = -1;

                            if (args.Length >= 3)
                            {
                                if (!Int32.TryParse(args[2], out groupId))
                                    groupId = -1;
                            }else if (sqlHandler.SelectedGroupId > 0)
                            {
                                groupId = SqlHandler.SelectedGroupId;
                            }

                            if (args.Length >= 4)
                            {
                                if (!Int32.TryParse(args[3], out taskId))
                                    taskId = -1;
                            }
                            else
                            {
                                taskId = sqlHandler.SelectedId;
                            }

                            if (sqlHandler.RemoveTaskFromGroup(groupId, taskId))
                            {
                                BestosoTaskTracker.PrintError("Failed to remove task from group!");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Task successfully removed from group!");
                            }

                            break;
                        }
                        case "rmgroup":
                        {
                            int groupId = -1;
                            if (args.Length >= 3)
                            {
                                if (!Int32.TryParse(args[2], out groupId))
                                    groupId = -1;
                            }

                            if (SqlHandler.DeleteGroup(groupId))
                            {
                                BestosoTaskTracker.PrintError("Failed to delete group!");
                            }
                            else
                            {
                                BestosoTaskTracker.PrintSuccess("Group successfully deleted!");
                            }

                            break;
                        }
                        case "tasks":
                        {
                            int groupId = -1;
                            if (args.Length >= 3)
                            {
                                if (!Int32.TryParse(args[2], out groupId))
                                    groupId = -1;
                            }else if (SqlHandler.SelectedGroupId > 0)
                            {
                                groupId = SqlHandler.SelectedGroupId;
                            }

                            if (SqlHandler.ShowTasksForGroup(groupId))
                            {
                                BestosoTaskTracker.PrintError("Failed to fetch tasks");
                            }

                            break;
                        }
                        case "select":
                        {
                             sqlHandler.SelectedGroupId = -1;
                             if (args.Length >= 3)
                             {
                                int id = -1;
                                if (!Int32.TryParse(args[2], out id))
                                {
                                    sqlHandler.SelectedGroupId = BestosoTaskTracker.GetUserInputInt("Select Group ID");
                                }
                                else
                                {
                                    sqlHandler.SelectedGroupId = id;
                                }
                             }
                             else
                             {
                                sqlHandler.SelectedGroupId = BestosoTaskTracker.GetUserInputInt("Select Group ID");
                             }
                             break;
                        }
                        case "deselect":
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[+] Deselected {sqlHandler.SelectedGroupId}");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            sqlHandler.SelectedGroupId = 0;
                            break;
                        }
                        default:
                        {
                            BestosoTaskTracker.PrintError("Invalid Input, Possible Commands Are : ");
                            BestosoTaskTracker.PrintStringList(groupCommands, null);
                            break;
                        }
                    }
                    break;
                }
                case "clearcompletedtasks":
                {
                    if (sqlHandler.DeleteCompletedTasks())
                    {
                        BestosoTaskTracker.PrintError("Failed clear completed tasks");
                    }
                    else
                    {
                        BestosoTaskTracker.PrintSuccess("Successfully cleared completed tasks");
                    }

                    break;
                }
                default:
                {
                    BestosoTaskTracker.PrintError("Invalid Command");
                    break;
                }
            }
            return ret;
        }
        #endregion
    }
}