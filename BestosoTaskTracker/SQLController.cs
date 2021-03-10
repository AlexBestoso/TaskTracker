using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;

namespace BestosoTaskTracker
{
    public class SQLController
    {
        #region Private Vars
        // Don't use an admin sql user for this connection string
        private const string host = "localhost";
        private const string databaseName = "BestosoTaskTracker";
        private string connectionString = "";
        private string username = null;
        private string password = null;
        private SqlConnection sqlConnection = null;
        private bool dbIsInitialized = false;
        private int selectedId = 0;
        private int selectedGroupId = 0;
        #endregion
        
        #region Public Vars
        public SqlConnection SqlConnection
        {
            get { return sqlConnection; }
        }
        public string UserName
        {
            get { return username; }
            set { username = value; }
        }
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        public bool DbIsInitialized
        {
            get { return dbIsInitialized; }
            set { dbIsInitialized = value; }
        }
        public int SelectedId
        {
            get { return selectedId; }
            set
            {
                if (value < 0)
                    selectedId = 0;
                else
                    selectedId = value;
            }
        }
        public int SelectedGroupId
        {
            get { return selectedGroupId; }
            set
            {
                if (value < 0)
                    selectedGroupId = 0;
                else
                    selectedGroupId = value;
            }
        }
        #endregion

        #region Setup Functions
        public bool InitTaskDataBase()
        {
            if (this.CreateSqlConnection() == true)
            {
                BestosoTaskTracker.PrintSuccess("Connection already established");
                return false;
            }

            Console.WriteLine("...Creating Database Connection...");
            
            this.UserName = BestosoTaskTracker.GetUserInput("Sql User", null, false);
            this.password = BestosoTaskTracker.GetMaskedInput("Sql Password");
            Console.Clear();

            if (this.CreateSqlConnection() == false)
            {
                BestosoTaskTracker.PrintError("Failed to establish sql connection");
                return true;
            }

            BestosoTaskTracker.PrintSuccess("Sql Connection Established!");
            if (this.CreateTaskDataBase() == false)
            {
                BestosoTaskTracker.PrintError("Failed to create database.");
            }

            if (this.DbIsInitialized)
            {
                Console.WriteLine("DB is setup.");
            }
            return false;
        }
        public bool CreateTaskDataBase()
        {
            bool ret = false;
            if (connectionString == null)
            {
                Console.WriteLine("Failed To find connection string");
                return ret;
            }

            string query = $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')\n"+
            "BEGIN\n"+
                $"\tCREATE DATABASE {databaseName}\n" +
            "END";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                    ret = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ret = false;
                }
            }
            sqlConnection.Close();

            connectionString = $"Data Source={host};Initial Catalog={databaseName};Integrated Security=False;User ID={username};Password={password};";
            sqlConnection = new SqlConnection(connectionString);

            query = "create table bestoso_tasks (" +
                        "id int IDENTITY(1,1) PRIMARY KEY," +
                        "task_name VARCHAR(1028)," +
                        "task_completed INT," +
                        "task_priority INT," +
                        "task_status VARCHAR(256)" +
                    ")";
            
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch(Exception ex)
                {
                    if (ex.Message.Contains("There is already an object named 'bestoso_tasks' in the database."))
                    {
                        BestosoTaskTracker.PrintSuccess("Found Main Task Table!"); 
                        ret = true;
                    }
                    else
                    {
                        BestosoTaskTracker.PrintError(ex.Message);
                    }
                }
            }
            sqlConnection.Close();

            query = "create table bestoso_groups(" +
                "id INT IDENTITY(1,1) PRIMARY KEY," +
                "task_count INT DEFAULT 0," +
                "group_name VARCHAR(256)," +
                "group_description VARCHAR(256)" +
                ""+
            ")";
            
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch(Exception ex)
                {
                    if (ex.Message.Contains("There is already an object named 'bestoso_groups' in the database."))
                    {
                        BestosoTaskTracker.PrintSuccess("Main Groups Table Detected!");
                        ret = true;
                    }
                    else
                    {
                        BestosoTaskTracker.PrintError(ex.Message);
                    }
                }
            }
            sqlConnection.Close();

            query = "create table bestoso_grouptask_join(" +
                    "id INT IDENTITY(1,1) PRIMARY KEY," +
                    "task_id INT FOREIGN KEY REFERENCES bestoso_tasks(id)," +
                    "group_id INT FOREIGN KEY REFERENCES bestoso_groups(id)" +
            ")";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch(Exception ex)
                {
                    if (ex.Message.Contains("There is already an object named 'bestoso_grouptask_join' in the database."))
                    {
                        BestosoTaskTracker.PrintSuccess("Main Groups Table Detected!");
                        ret = true;
                    }
                    else
                    {
                        BestosoTaskTracker.PrintError(ex.Message);
                    }
                }
            }
            sqlConnection.Close();
            
            return ret;
        }
        public bool CreateSqlConnection()
        {
            if (username == null)
            {
               
                return false;
            }

            if (password == null)
            {
                return false;
            }

            if (sqlConnection != null)
            {
                Console.WriteLine("Database Already Connected.");
                return true;
            }
            
            connectionString = $"Data Source={host};Integrated Security=False;User ID={username};Password={password};";
            try
            {
                sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();
                if (sqlConnection.State == ConnectionState.Closed)
                {
                    sqlConnection = null;
                    return false;
                }
                sqlConnection.Close();
                return true;
            }
            catch (Exception ex)
            {
                sqlConnection = null;
                if (ex.Message.Contains("Login failed for user"))
                {
                    BestosoTaskTracker.PrintError("Failed To Authenticate To SQL Server!");
                }
                else
                {
                    Console.WriteLine(ex);
                }

                return false;
            }
        }
        #endregion
        
        #region Task Functions
        public bool DeleteCompletedTasks()
        {
            Console.WriteLine("Clearing out all completed tasks");
            Console.WriteLine("\tType exit to cancel");
            Console.Write(":)> ");
            string grabber = Console.ReadLine();
            
            if (grabber == "exit")
            {
                return false;
            }
            
            string query = $"DELETE FROM bestoso_tasks WHERE task_completed=1";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    sqlConnection.Close();
                    return true;
                }
            }
            sqlConnection.Close();
            return false;
        }
        public string GetTaskName(int id)
        {
            string ret = "";
            string query = $"SELECT task_name FROM bestoso_tasks WHERE id = {id}";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    if (sdr.Read())
                    {
                        ret = (string)sdr["task_name"];
                    }
                }
            }
            sqlConnection.Close();
            return ret;
        }
        public bool GetTaskDetails()
        {
            int id = SelectedId <= 0 ? BestosoTaskTracker.GetUserInputInt("Select Task ID") : SelectedId;
            if (id <= 0)
            {
                return true;
            }
                    
            string selectedTaskName = GetTaskName(id);
                    
            Console.Write("Task details for : \t");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(selectedTaskName);
            Console.ForegroundColor = ConsoleColor.Yellow;
            
            string query = "SELECT * FROM bestoso_tasks WHERE task_completed=0 AND id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        int priority = (int)sdr["task_priority"];
                        string status = (string)sdr["task_status"];
                        
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("Priority\t :\t");
                        Console.ForegroundColor = ConsoleColor.Green; 
                        Console.WriteLine($"{priority}");
                        
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"Status\t\t :\t");
                        Console.ForegroundColor = ConsoleColor.Green; 
                        Console.WriteLine($"{status}");
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            sqlConnection.Close();
            return false;
        }
        public bool OffloadTask()
        {
            int id = SelectedId <= 0 ? BestosoTaskTracker.GetUserInputInt("Select Task ID") : SelectedId;
            if (id <= 0)
            {
                return true;
            }
            SelectedId = 0;
            
            string query = "UPDATE bestoso_tasks SET task_priority=0 WHERE id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return true;
                }
            }
            sqlConnection.Close();
            return false;
        }
        public bool GetTasksWithNoGroups()
        {
            bool ret = false;
            string query = "SELECT * FROM bestoso_tasks WHERE task_completed=0 AND id NOT IN (SELECT task_id FROM bestoso_grouptask_join)";
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"   id|\ttask name|\tpriority|\tstatus|\n");
                   
                        int colorFormatter = 0;
                        while (sdr.Read())
                        {
                            if ((colorFormatter % 2) == 0)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            else{
                                Console.BackgroundColor = ConsoleColor.Magenta;
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            int priority = (int)sdr["task_priority"];
                            string taskname = (string)sdr["task_name"];
                            string status = (string)sdr["task_status"];
                            int id = (int) sdr["id"];

                            if (priority <= 0)
                            {
                                Console.BackgroundColor = ConsoleColor.Red;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }

                            if (priority >= 10)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGreen;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }

                            Console.WriteLine($"[-] {id}|\t{taskname}|\t{priority}|\t{status}|");
                            colorFormatter++;
                        }

                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                ret = true;
            }
            sqlConnection.Close();
            return ret;
        }
        public bool GetOpenTasks()
        {
            string query = "SELECT * FROM bestoso_tasks WHERE task_completed = 0 ORDER BY task_priority ASC";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    
                    Console.WriteLine($"   id|\ttask name|\tpriority|\tstatus|\n");
                   
                    int colorFormatter = 0;
                    while (sdr.Read())
                    {
                        if ((colorFormatter % 2) == 0)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        else{
                            Console.BackgroundColor = ConsoleColor.Magenta;
                            Console.ForegroundColor = ConsoleColor.White;
                        }

                        int priority = (int)sdr["task_priority"];
                        string taskname = (string)sdr["task_name"];
                        string status = (string)sdr["task_status"];
                        int id = (int) sdr["id"];

                        if (priority <= 0)
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }

                        if (priority >= 10)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }

                        Console.WriteLine($"[-] {id}|\t{taskname}|\t{priority}|\t{status}|");
                        colorFormatter++;
                    }

                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
            }
            sqlConnection.Close();
            return false;
        }
        public bool MarkTaskCurrent(int taskId = -1)
        {
            bool ret = false;
            if (taskId <= -1)
            {
                taskId = BestosoTaskTracker.GetUserInputInt("Please Enter A Task ID");
                if(taskId <= -1){
                    return true;
                }
            }
            
            string query = "UPDATE bestoso_tasks SET task_priority=10 WHERE id=@taskId";
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.Add(new SqlParameter("@taskId", taskId));
                    ret = false;
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        ret = true;
                    }
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
            }
            sqlConnection.Close();
            return ret;
        }
        public bool GetClosedTasks()
        {
            string query = "SELECT * FROM bestoso_tasks WHERE task_completed = 1";
            try
            {
                sqlConnection.Open();
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"   id|\ttask name|\tpriority|\tstatus|");
                        while (sdr.Read())
                        {
                            int priority = (int) sdr["task_priority"];
                            string taskname = (string) sdr["task_name"];
                            string status = (string) sdr["task_status"];
                            int id = (int) sdr["id"];

                            Console.WriteLine($"[+] {id}|\t{taskname}|\t{priority}|\t{status}|");
                        }
                    }
                }

                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }

            return false;
        }
        public bool SetTaskStatus()
        {
            int id = SelectedId <= 0 ? BestosoTaskTracker.GetUserInputInt("Select Task ID") : SelectedId;
            if (id <= 0)
            {
                return true;
            }
            string selectedTaskName = GetTaskName(id);
                    
            string taskStatus = BestosoTaskTracker.GetUserInput("Please Enter A New Task Status", selectedTaskName);
            if (taskStatus == null)
            {
                return true;
            }
            
            string query = $"UPDATE bestoso_tasks SET task_status=@status WHERE id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@status", taskStatus);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    BestosoTaskTracker.PrintError(ex.Message);
                    return true;
                }
            }
            sqlConnection.Close();
            return false;
        }
        public bool SetTaskPriority()
        {
            bool ret = false;
            int id;
            int priority;

            if (selectedId <= 0)
            {
                id = BestosoTaskTracker.GetUserInputInt("please Provide a Task ID");
                if (id < 0)
                {
                    return true;
                }
            }
            else
            {
                id = selectedId;
            }

            priority = BestosoTaskTracker.GetUserInputInt($"Please enter a new priority number for task '{GetTaskName(id)}'");
            if (priority < 0)
            {
                return true;
            }

            string query = $"UPDATE bestoso_tasks SET task_priority=@priority WHERE id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@priority", priority);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    ret = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ret = true;
                }
            }
            sqlConnection.Close();
            return ret;
        }
        public bool ReloadOffloadedTasks(int priority=-1)
        {
            bool ret = false;
            if (priority <= -1)
            {
                priority = BestosoTaskTracker.GetUserInputInt("Please Enter A New Priority");
                if(priority <= -1){
                    return true;
                }
            }

            string query = "update bestoso_tasks SET task_priority=@priority WHERE task_completed=0 AND task_priority<=0";

            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@priority", priority);
                    cmd.ExecuteNonQuery();
                    ret = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ret = true;
                }
            }
            sqlConnection.Close();
            return ret;
        }
        public bool SetTaskAsUncomplete()
        {
            bool ret = false;
            int id;
            string grabber = "";

            if (selectedId <= 0)
            {
                id = BestosoTaskTracker.GetUserInputInt("Please Provide a task ID");
                if (id <= 0)
                {
                    return true;
                }
            }
            else
            {
                id = selectedId;
            }

            grabber = BestosoTaskTracker.GetUserInput($"Setting task '{GetTaskName(id)}' to completed");
            if (grabber == null)
            {
                return true;
            }
            
            string query = $"UPDATE bestoso_tasks SET task_completed=0, task_status='in progress' WHERE id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    ret = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ret = true;
                }
            }
            sqlConnection.Close();
            return ret;
        }
        public bool SetTaskAsComplete()
        {
            bool ret = false;
            int id;
            string grabber = "";
            
            if (selectedId <= 0)
            {
                id = BestosoTaskTracker.GetUserInputInt("Provide A Task ID");
                if (id <= 0)
                {
                    return true;
                }
            }
            else
            {
                id = selectedId;
            }

            grabber = BestosoTaskTracker.GetUserInput($"Setting task '{GetTaskName(id)}' to completed");
            if (grabber == null)
            {
                return true;
            }

            string query = $"UPDATE bestoso_tasks SET task_completed=1, task_status='done' WHERE id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    ret = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ret = true;
                }
            }
            sqlConnection.Close();
            return ret;
        }
        public bool GetOffloadedTasks()
        {
            string query = "SELECT * FROM bestoso_tasks WHERE task_priority = 0 AND task_completed = 0";
            try
            {
                sqlConnection.Open();
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"   id|\ttask name|\tpriority|\tstatus|");
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        while (sdr.Read())
                        {
                            int priority = (int) sdr["task_priority"];
                            string taskname = (string) sdr["task_name"];
                            string status = (string) sdr["task_status"];
                            int id = (int) sdr["id"];

                            Console.WriteLine($"[-] {id}|\t{taskname}|\t{priority}|\t{status}|");
                        }
                    }
                }

                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Yellow;
                return true;
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;
            return false;
        }
        public bool ExecuteUpdateTaskName()
        {
            int id = SelectedId <= 0 ? BestosoTaskTracker.GetUserInputInt("Select Task ID") : SelectedId;
            if (id <= 0)
            {
                return true;
            }

            string selectedTaskName = GetTaskName(id);
                    
            string taskname = BestosoTaskTracker.GetUserInput("Please Enter A New Task Name", selectedTaskName);
            if (taskname == null)
            {
                return true;
            }
            
            string query = "UPDATE bestoso_tasks set task_name=@newName WHERE id=@id";
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@newName", taskname);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    sqlConnection.Close();
                    BestosoTaskTracker.PrintError(ex.Message);
                    return true;
                }
            }

            sqlConnection.Close();
            return false;
        }
        public bool ExecuteInsertTask()
        {
            string taskStatus = "Not Started";
            int taskPriority = -1;
            string taskName = null;
            bool taskCompleted = false;
            
            taskName = BestosoTaskTracker.GetUserInput("Enter New Tasks' Name");
            if (taskName == null)
            {
                return true;
            }

            taskPriority = BestosoTaskTracker.GetUserInputInt("Enter Task Priority (An Integer)");
            if (taskPriority <= -1)
            {
                return true;
            }
            
            string query = $"INSERT INTO bestoso_tasks (task_name, task_completed, task_priority, task_status) OUTPUT Inserted.ID VALUES(@taskname, @taskcompleted, @priority, @status)";
            int newTaskId = 0;
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query,sqlConnection))
            {
                cmd.Parameters.AddWithValue("@taskname", taskName);
                cmd.Parameters.AddWithValue("@taskcompleted", ((taskCompleted == false) ? 0 : 1));
                cmd.Parameters.AddWithValue("@priority", taskPriority);
                cmd.Parameters.AddWithValue("@status", taskStatus);
                using (SqlDataReader sdr = cmd.ExecuteReader())
                {
                    while (sdr.Read())
                    {
                        newTaskId = (int)sdr["ID"];
                    }
                }
            }
            sqlConnection.Close();

            if (this.selectedGroupId > 0)
            {
                string inpt = BestosoTaskTracker.GetUserInput("Add task to group? (y/N)");
                if (inpt == "y")
                {
                    AddTaskToGroup(newTaskId, this.selectedGroupId);
                }
            }

            return false;
        }
        #endregion

        #region Group Functions
        public bool CreateNewBestosoGroup(string groupName = null, string groupDescription = null)
        {
            if (groupName == null)
            {
                groupName = BestosoTaskTracker.GetUserInput("Group Name");
                if (groupName == null)
                {
                    return true;
                }
            }

            if (groupDescription == null)
            {
                groupDescription = BestosoTaskTracker.GetUserInput("Group Description");
                if (groupDescription == null)
                {
                    return true;
                }
            }

            /*
             * Parameters are prepped, create the group
             */

            string query = "INSERT INTO bestoso_groups(" + 
                "group_name, " +
                "group_description, " +
                "task_count" +
            ") VALUES (" +
                "@groupName, " +
                "@groupDescription, " +
                "0" +
            ")";

            bool ret = false;
            sqlConnection.Open();
            using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
            {
                try
                {
                    cmd.Parameters.AddWithValue("@groupName", groupName);
                    cmd.Parameters.AddWithValue("@groupDescription", groupDescription);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    BestosoTaskTracker.PrintError(ex.Message);
                    ret = true;
                }
            }
            sqlConnection.Close();
            return ret;
        }
        public bool AddTaskToGroup(int taskId=-1, int groupId=-1)
        {
            if (taskId <= -1)
            {
                taskId = BestosoTaskTracker.GetUserInputInt("Enter a task ID");
                if (taskId <= -1)
                {
                    return true;
                }
            }

            if (groupId <= -1)
            {
                groupId = BestosoTaskTracker.GetUserInputInt("Enter a group ID");
                if (groupId <= -1)
                {
                    return true;
                }
            }

            string query = "INSERT INTO bestoso_grouptask_join (task_id, group_id) VALUES(@taskId, @groupId)";
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }
            sqlConnection.Close();

            int count = 0;
            query = "SELECT COUNT(*) FROM bestoso_grouptask_join as gt INNER JOIN bestoso_tasks as bt ON bt.id = gt.task_id WHERE group_id=@groupId AND bt.task_completed = 0";
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    count = (Int32)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }
            sqlConnection.Close();

            query = "UPDATE bestoso_groups SET task_count=@count WHERE id=@groupId";
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@count", count);
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }
            sqlConnection.Close();

            return false;
        }
        public bool DeleteGroup(int groupId=-1)
        {
            if (groupId <= -1)
            {
                groupId = BestosoTaskTracker.GetUserInputInt("Enter a group id");
                if (groupId <= -1)
                {
                    return true;
                }
            }
            
            // Delete all relative joins. 
            string query = "DELETE FROM bestoso_grouptask_join WHERE group_id=@groupId";
            try
            {
                sqlConnection.Open();
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.ExecuteNonQuery();
                }

                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }

            // Delete the actual group
            query = "DELETE FROM bestoso_groups WHERE id=@groupId";
            try
            {
                sqlConnection.Open();
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.ExecuteNonQuery();
                }
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }

            return false;
        }
        public bool ShowTasksForGroup(int groupId = -1)
        {
            if (groupId <= -1)
            {
                groupId = BestosoTaskTracker.GetUserInputInt("Enter a group ID");
                if (groupId <= -1)
                {
                    return true;
                }
            }

            string query = "SELECT * FROM bestoso_tasks AS bt " +
                           "INNER JOIN bestoso_grouptask_join AS gt "+
                           "ON bt.id = gt.task_id "+
                           "WHERE gt.group_id=@groupId AND bt.task_completed=0 ORDER BY bt.task_priority ASC";

            try
            {
                sqlConnection.Open();
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"   id|\ttask name|\tpriority|\tstatus|\n");
                   
                        int colorFormatter = 0;
                        while (sdr.Read())
                        {
                            if ((colorFormatter % 2) == 0)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            else{
                                Console.BackgroundColor = ConsoleColor.Magenta;
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            int priority = (int)sdr["task_priority"];
                            string taskname = (string)sdr["task_name"];
                            string status = (string)sdr["task_status"];
                            int id = (int) sdr["id"];

                            if (priority <= 0)
                            {
                                Console.BackgroundColor = ConsoleColor.Red;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }

                            if (priority >= 10)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGreen;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }

                            Console.WriteLine($"[-] {id}|\t{taskname}|\t{priority}|\t{status}|");
                            colorFormatter++;
                        }
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                }
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                return true;
            }

            return false;
        }
        public bool ShowBestosoGroups()
        {
            bool ret = false;
            string query = "SELECT (SELECT count(*) FROM bestoso_tasks INNER JOIN bestoso_grouptask_join AS gt ON bestoso_tasks.id = gt.task_id WHERE task_completed=0 AND gt.group_id = bestoso_groups.id) as count,* FROM bestoso_groups";
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        Console.WriteLine($"    id|\tgroup name|\tcount|\tDescription|\n");
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        int colorFormatter = 0;
                        while (sdr.Read())
                        {
                            if ((colorFormatter % 2) == 0)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            else
                            {
                                Console.BackgroundColor = ConsoleColor.Magenta;
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            string groupName = (string) sdr["group_name"];
                            int id = sdr["id"] == DBNull.Value ? -1 : (int) sdr["id"];
                            int count = (int)sdr["count"];
                            string description = (string) sdr["group_description"];

                            Console.WriteLine($"[-] {id}|\t{groupName}|\t{count}|\t{description}|");
                            colorFormatter++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                ret = true;
            }
            sqlConnection.Close();
            
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;
            return ret;
        }
        public bool RemoveTaskFromGroup(int groupId = -1, int taskId = -1)
        {
            bool ret = false;
            if (groupId <= 0)
            {
                groupId = BestosoTaskTracker.GetUserInputInt("Enter group ID");
                if (groupId <= 0)
                {
                    return true;
                }
            }

            if (taskId <= 0)
            {
                taskId = BestosoTaskTracker.GetUserInputInt("Enter task ID");
                if (taskId <= 0)
                {
                    return true;
                }
            }

            string query = "DELETE FROM bestoso_grouptask_join WHERE group_id=@groupId AND task_id=@taskId";
            
            sqlConnection.Open();
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, sqlConnection))
                {
                    cmd.Parameters.AddWithValue("@groupId", groupId);
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                BestosoTaskTracker.PrintError(ex.Message);
                ret = true;
            }
            sqlConnection.Close();

            return ret;
        }
        #endregion
    }
}