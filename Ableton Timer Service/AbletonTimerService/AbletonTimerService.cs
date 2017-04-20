/*  STRIDER WHITE 2017
 *  
 *  Ableton Timer is a automatic service which records how long Ableton Live has been running for
 *  FEATURES: 
 *      * Automatic file and directory integrety checks and repairal 
 *      * Verbose text dumps indicating session start time, session end time, and span of session. Files for each session and a main info file.
 *      * Serialzation of complex List<Sessions> object
 * 
 *  PLANNED:
 *      * Easy CSV file generated from data files 
 *      * Backup file
 *  
 *  SOME CODE USED FROM THE FOLLOWING RESOURCES:
 *  http://stackoverflow.com/questions/4864673/windows-service-to-run-constantly
 *  http://stackoverflow.com/questions/194157/c-sharp-how-to-get-program-files-x86-on-windows-64-bit
 *  https://msdn.microsoft.com/en-us/library/
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace AbletonTimerService
{
    public partial class AbletonTimerService : ServiceBase
    {
        Thread timerThread; //Main exeuction thread
        bool monitor;       //Control bool for execution thread


        public AbletonTimerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Launch our service thread
            timerThread = new Thread(ThreadSessionMonitor);
            timerThread.Name = "Monitor Ableton Thread";
            timerThread.IsBackground = true;
            monitor = true;
            timerThread.Start();

        }

        public void ThreadSessionMonitor()
        {
            //Various file and directory vars
            string programDirectory = ((Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) + "\\Ableton Timer");
            string sessionsDirectory = programDirectory + "\\sessions\\";
            string dataFilePath = programDirectory + "\\TimerData.dat";
            string backupFilePath = programDirectory + "\\BackupData.dat";

            //Thread.Sleep(1000 * 30);

            //Debug.Write(programDirectory);
            //Debug.Write(sessionsDirectory);
            //Debug.Write(dataFilePath);
            //Debug.Write(backupFilePath);

            //More vars
            List<Session> sessions;
            Session currentSession = null;
            bool record = false;
            monitor = true;



            //Directory & Data Integriy Check
            //Make sure the program directory exists, if not, make it
            if (!Directory.Exists(programDirectory))
                Directory.CreateDirectory(programDirectory);

            //Make sure the sessions directory exists, if not, make it
            if (!Directory.Exists(sessionsDirectory))
                Directory.CreateDirectory(sessionsDirectory);


            //Check main data file, creates if doesnt exist
            if (!File.Exists(dataFilePath))
                UpdateMasterInfoFile(programDirectory, new List<Session>());

            //Get the current state of sessions
            sessions = getSessionsList(dataFilePath);


            //Main logic loop
            while (monitor)
            {
                //Wait a bit, dont be greedy
                Thread.Sleep(1000);

                //Check if ableton is running
                if (AbletonRunning())
                {
                    //start recording if we arent
                    if (!record)
                    {
                        record = true;
                        currentSession = new Session(sessions.Count());
                    }

                }
                //Ableton is not running
                else
                {
                    //If ableton is not running and we were recording a session, it means ableton stopped running.
                    //In this case stop all session recording and write all the data to a session text file & update master file
                    if (record)
                    {
                        record = false;
                        currentSession.saveSession();
                        sessions.Add(currentSession);
                        SaveSession(currentSession, sessionsDirectory);
                        UpdateMasterInfoFile(programDirectory, sessions);
                    }
                }
            }
        }
        /// <summary>
        /// Creates a new session text file
        /// </summary>
        private void SaveSession(Session session, string path)
        {
            //Stream sessionFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            //StreamWriter streamWriter = new StreamWriter(sessionFileStream);
            string[] lines = { "Session # " + session.sessionNumber,
                               "Session Date: " + session.startOfSession.Date.ToString("d"),
                               "Session Span (hr/min/sec): " + string.Format("{0:hh\\:mm\\:ss}",session.sessionTimeSpan.Duration())};

            try
            {
                File.WriteAllLines(path + "Log_Session_" + session.sessionNumber + ".txt", lines);
            }
            catch
            {
                //todo: implement a EventLogging system...
            }

            //FileStream fileStream = 
            //BinaryFormatter formatter = new BinaryFormatter();
            //formatter.Serialize(sessionFileStream, sessions);

            //sessionFileStream.Close();
        }


        /// <summary>
        /// Update (overwrite) the master data file by serialzing the current sessions list
        /// </summary>
        private void UpdateMasterInfoFile(string path, List<Session> sessions)
        {
            //serialize and save the main data file
            Stream masterFileStream = new FileStream(path + "\\TimerData.dat", FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(masterFileStream, sessions);
            masterFileStream.Close();

            //Calculate and write some info information to a file...

            //the message
            string[] lines;

            //if we dont have at least 2 sessions dont do any average calculations
            if (sessions.Count > 1)
            {
                //Determine aveage session in minutes
                double average = sessions.Average(o => o.sessionTimeSpan.TotalMinutes);


                //make up a string....
                lines = new string[]{
                                "*******************************",
                                "ABLETON TIMER MASTER LOG",
                                "*******************************",
                                "Total # of sessions: " + sessions.Count,
                                "Longest session (hr/min/sec):" + sessions.OrderBy( o=> o.sessionTimeSpan.Duration() ).Last().sessionTimeSpan.ToString(@"hh\:mm\:ss"),
                                "Shortest session (hr/min/sec): " + sessions.OrderBy(o=>o.sessionTimeSpan.Duration()).First().sessionTimeSpan.ToString(@"hh\:mm\:ss"),
                                "Average session: " + average.ToString("F0") + " minutes"};
            }
            else
            {
                //Tell them we need at least 2 sessions
                lines = new string[] { "Need at least 2 sessions on record... start another Ableton session." };
            }

            //Now write out some useful data stats to a "main" text file
            try
            {
                File.WriteAllLines(path + "\\Master Log" + ".txt", lines);
            }
            catch
            {
                //todo: implement event logging....
            }

        }

        private List<Session> getSessionsList(string path)
        {
            Stream masterFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            //if the file was empty, make a new List<Session>
            if (masterFileStream.Length == 0)
            {
                masterFileStream.Close();
                return new List<Session>();
            }
            else
            {
                BinaryFormatter formatter = new BinaryFormatter();
                List<Session> sessions = (List<Session>)formatter.Deserialize(masterFileStream);
                masterFileStream.Close();
                return sessions;
            }
        }


        /// <summary>
        /// Runs when process is stopping
        /// </summary>
        protected void OnStop()
        {
            monitor = false;
            if (!timerThread.Join(3000))
            { // give the thread 3 seconds to stop
                timerThread.Abort();
            }
        }


        ///Lets us know if Ableton is running
        static bool AbletonRunning()
        {
            return Process.GetProcessesByName("Ableton Live 9 Suite").Count() > 0;
        }


        ///// <summary>
        ///// Helps get the window profile enviroment variable 
        ///// </summary>
        ///// <returns></returns>
        //static string ProgramFilesx86()
        //{
        //    if (8 == IntPtr.Size
        //        || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
        //    {
        //        return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        //    }

        //    return Environment.GetEnvironmentVariable("ProgramFiles");
        //}
    }
}
