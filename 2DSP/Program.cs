using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace _2DSP
{
    class Program
    {
        private static string _scheduleFileName = "schedule.txt";
        // NFDH - Next fit decreasing height, FFDH - First fit decreasing height
        // GENERATE - generate tasks
        public enum Mode { InvalidMode, NFDH, FFDH, GENERATE }
        private static Mode _mode;
        private static int _emNumber; // Number of elementary machines in system (width of strip)
        private static Stopwatch watch = new Stopwatch();

        static void Main(string[] args)
        {
            try
            {
                List<Task> tasks = new List<Task>();
                _mode = ParseMode(args[0]);
                watch.Start();
                
                Execute(args, tasks);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.GetType().FullName);
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Execute operation (generate tasks / NFDH / FFDH)
        /// </summary>
        /// <param name="args">main string arguments</param>
        /// <param name="tasks">List collection of Tasks</param>
        public static void Execute(string[] args, List<Task> tasks)
        {
            if (tasks == null) throw new ArgumentNullException();

            if (_mode == Mode.InvalidMode)
            {
                return; // throw new InvalidModeException();
            }
            else
            {
                if (_mode == Mode.GENERATE)
                {
                    int tasksCount = int.Parse(args[1]);
                    int maxEMNumber = int.Parse(args[2]);
                    int maxTime = int.Parse(args[3]);
                    GenerateTasks(tasksCount, maxEMNumber, maxTime);
                }
                else
                {
                    _emNumber = int.Parse(args[2]);
                    string tasksFileName = args[1];

                    InitializeTaskList(tasksFileName, tasks);

                    if (_mode == Mode.FFDH)
                    {
                        FFDH(tasks);
                    }
                    else
                    {
                        if (_mode == Mode.NFDH)
                            NFDH(tasks);
                    }
                }
            }
        }

        /// <summary>
        /// Parsing Mode
        /// </summary>
        /// <param name="mode">parsing mode string</param>
        /// <returns>Mode value, returns InvalidMode if string does no match to any of Mode values</returns>
        public static Mode ParseMode(string mode)
        {
            if (mode.Equals(Mode.GENERATE.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return Mode.GENERATE;
            }
            else
            {
                if (mode.Equals(Mode.NFDH.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return Mode.NFDH;
                }
                else
                {
                    if (mode.Equals(Mode.FFDH.ToString(), StringComparison.OrdinalIgnoreCase))
                        return Mode.FFDH;
                }
            }
            return Mode.InvalidMode;
        }

        /// <summary>
        /// Initializing List of Tasks (from file) and sort elements
        /// </summary>
        /// <param name="tasksFileName">name or/and path of file containing Tasks</param>
        /// <param name="tasks">list of Tasks to init and sort</param>
        public static void InitializeTaskList(string tasksFileName, List<Task> tasks)
        {
            if (tasks == null || tasksFileName == null) throw new ArgumentNullException();

            using (StreamReader tasksFile = new StreamReader(tasksFileName))
            {
                // List initialization with information from file
                string[] row;
                int taskNumber = 1, emNumber, time;
                while (!tasksFile.EndOfStream)
                {
                    row = tasksFile.ReadLine().Split(' ');
                    emNumber = int.Parse(row[0]);
                    time = int.Parse(row[1]);
                    tasks.Add(new Task(emNumber, time, taskNumber));
                    taskNumber++;
                }

                // Sort the rectangles in order of non-increasing height such that h(L1) ≥ h(L2) ≥ ... ≥ h(Ln)
                tasks.Sort((x, y) => y.Time.CompareTo(x.Time));
            }
        }

        /// <summary>
        /// Next Fit Decreasing Height 2-dimensional strip packaging algorithm. Creates schedule.
        /// </summary>
        /// <param name="tasks">list of Tasks for creating a schedule</param>
        public static void NFDH (List<Task> tasks) // Next Fit Decreasing High algorithm
        {
            if (tasks == null) throw new ArgumentNullException();

            int levelNumber = 0;
            List<Level> levels = new List<Level>();
            
            levels.Add(new Level(tasks[0].Time, levelNumber)); // Add the first Level and set its height 

            levels[levelNumber].AddTask(tasks[0]); // Add the first task to the first level (bottom-left side of strip)
            for (int i = 1; i < tasks.Count; i++)
            {
                
                if(_emNumber - levels[levelNumber].Width >= tasks[i].EMCount)
                {
                    // pack rectangle Li to the right of rectangle i-1
                    //levels[levelNumber].AddTask(TasksList[i]);
                    AddTask(levels[levelNumber], tasks[i]);
                }
                else
                {
                    // create a new level above the previous one and pack rectangle Li on the new level
                    levelNumber++;
                    levels.Add(new Level(tasks[levelNumber].Time + levels[levelNumber - 1].Height, levelNumber));
                    //levels[levelNumber].AddTask(TasksList[i]);
                    AddTask(levels[levelNumber], tasks[i]);
                }
            }
            Result(levels, tasks);
        }

        /// <summary>
        /// First Fit Decreasing Height 2-dimensional strip packaging algorithm. Creates schedule.
        /// </summary>
        /// <param name="tasks">list of Tasks for creating a schedule</param>
        public static void FFDH(List<Task> tasks) // First Fit Decreasing High algorithm
        {
            if (tasks == null) throw new ArgumentNullException();

            int levelNumber = 0, level;
            List<Level> levels = new List<Level>();

            levels.Add(new Level(tasks[0].Time, levelNumber)); // Add the first Level and set its height 

            levels[levelNumber].AddTask(tasks[0]); // Add the first task to the first level (bottom-left side of strip)
            for (int i = 1; i < tasks.Count; i++)
            {
                for (level = 0; level < levelNumber; level++) // trying to found some free space for this rectangle on previous levels
                {
                    if (_emNumber - levels[level].Width >= tasks[i].EMCount)
                    {                      
                        levels[level].AddTask(tasks[i]);
                        break;
                    }
                }

                if (level < levelNumber) continue; // Current task already in place, need to take next one

                if (_emNumber - levels[levelNumber].Width >= tasks[i].EMCount)
                {
                    // pack rectangle Li to the right of rectangle i-1
                    levels[levelNumber].AddTask(tasks[i]);
                }

                else
                {
                    // create a new level above the previous one and pack rectangle Li on the new level
                    levelNumber++;
                    levels.Add(new Level(tasks[levelNumber].Time + levels[levelNumber - 1].Height, levelNumber));
                    levels[levelNumber].AddTask(tasks[i]);
                }
            }
            Result(levels, tasks);
        }

        /// <summary>
        /// Adds task to the specified level
        /// </summary>
        /// <param name="level">level for task to add</param>
        /// <param name="task">task that should be added to the level</param>
        public static void AddTask(Level level, Task task)
        {
            task.StartEM = level.Width;
            level.AddTask(task);
        }

        /// <summary>
        /// Prints result of program execution to the console window
        /// </summary>
        /// <param name="levels">levels list</param>
        /// <param name="tasks">tasks list</param>
        public static void Result(List<Level> levels, List<Task> tasks)
        {
            double t = 0, e, ts = levels[levels.Last().Number].Height;

            for (int i = 0; i < tasks.Count; i++)
            {
                t += tasks[i].EMCount * tasks[i].Time;
            }
            t = t / _emNumber;
            e = (ts - t) / t;

            watch.Stop();

            Console.WriteLine("Result{2}Using algorithm: {0}{2}N: {1}", 
                _mode, _emNumber, Environment.NewLine);
            Console.WriteLine("Tasks count: {0}{3}T(S): {1}{3}E: {2}", tasks.Count, ts, e, Environment.NewLine);
            Console.WriteLine("Time: {0} ms{2}{2}Tasks schedule in file {1}", 
                watch.ElapsedMilliseconds, _scheduleFileName, Environment.NewLine);

            using (StreamWriter output = new StreamWriter(_scheduleFileName))
            {
                foreach (Level level in levels)
                {
                    foreach (Task task in level.TasksList)
                    {
                        output.WriteLine("{{task №{0}, StartEM: {1} EMCount: {2}, Time: {3}, L: {4}}}", 
                            task.Number, task.StartEM, task.EMCount, task.Time, level.Number);
                    }
                }
            }
        }

        /// <summary>
        /// Generates random tasks and writes them to the file
        /// </summary>
        /// <param name="tasksCount">count of tasks to generate</param>
        /// <param name="maxEMNumber">max elementary machines number that tasks can require</param>
        /// <param name="maxTime">max time that tasks can execute</param>
        public static void GenerateTasks(int tasksCount, int maxEMNumber, int maxTime)
        {
            Random random = new Random();
            int emNumber, time;

            using(StreamWriter outStream = new StreamWriter(string.Format("tasks{0}.txt", tasksCount.ToString())))
            {
                for (int i = 0; i < tasksCount; i++)
                {
                    emNumber = random.Next(1, maxEMNumber);
                    time = random.Next(1, maxTime);
                    outStream.WriteLine("{0} {1}", emNumber.ToString(), time.ToString()); //tasks file format: "width height"
                }
            }

            watch.Stop();
        }
    }

    /// <summary>
    /// Task representation
    /// </summary>
    public class Task
    {
        public int Number   // Number of the task in the file
        { get; set; }
        public int EMCount  // Number of elemental machines required for execution
        { get; set; }

        public int StartEM  // Number of the first EM that this task assigned to 
        { get; set; }

        public int Time     // Amount of time is required for execution
        { get; set; }

        public Task (int width, int height, int number)
        {
            EMCount = width;
            Time = height;
            Number = number;
        }
    }

    /// <summary>
    /// Level representation
    /// </summary>
    public class Level
    {
        private int _width;             // Level width
        public int Number { get; set; } // Number of the level
        public int Width                // Width of level (number of elementary machines)
        {
            get { return _width; }
        }
        public int Height               // Level height
        { get; }

        public List<Task> TasksList;    // List of tasks

        public Level(int height, int number)
        {
            _width = 0;
            Height = height;
            Number = number;
            TasksList = new List<Task>();
        }

        public void AddTask(Task task)
        {
            TasksList.Add(task);
            _width += task.EMCount;
        }
    }
}
