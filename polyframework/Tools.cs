using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Core
{
    public class Tools
    {

        static List<string> logList = new List<string>();
 
        public static void Log(string message)
        {
            logList.Add(message);
        }

        public static void WriteLogListToFile()
        {
            System.IO.File.WriteAllText("DelayedLogfile.txt", "This is the file DelayedLogfile.txt\n");
            System.IO.File.AppendAllText("DelayedLogfile.txt", "\n----------------------------\nWriteLogListToDebugLog begin\n----------------------------\n");

            foreach (var item in logList)
                System.IO.File.AppendAllText("DelayedLogfile.txt", item + "\n");
            System.IO.File.AppendAllText("DelayedLogfile.txt", "--------------------------\nWriteLogListToDebugLog end\n--------------------------\n");
        }

    }
}
