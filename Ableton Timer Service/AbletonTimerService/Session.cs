using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AbletonTimerService
{
    [Serializable()]
    public class Session
    {
        /// <summary>
        /// state vars
        /// </summary>
        public DateTime startOfSession;
        public DateTime endOfSession;
        [NonSerialized]
        public Stopwatch stopwatch;
        public TimeSpan sessionTimeSpan;
        public int sessionNumber;

        /// <summary>
        /// On create, start recording and get the current DateTime
        /// </summary>
        public Session(int seshNumber)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            startOfSession = DateTime.Now;
            sessionNumber = seshNumber;
        }

        /// <summary>
        /// save the session by updating state vars 
        /// </summary>
        public void saveSession()
        {
            stopwatch.Stop();
            sessionTimeSpan = stopwatch.Elapsed;
            endOfSession = DateTime.Now;
        }

    }
}
