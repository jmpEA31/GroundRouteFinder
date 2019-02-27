using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroundRouteFinder.LogSupport
{
    public static class Logger
    {
        private static string fileName;

        public static void CreateLogfile(string path)
        {
            fileName = Path.Combine(path, "logfile.txt");
            using (StreamWriter sw = File.CreateText(fileName))
            {
                sw.WriteLine("Ground Route Generator Log");
            }
        }

        public static void Log(string message)
        {
            using (StreamWriter sw = File.AppendText(fileName))
            {
                sw.WriteLine(message);
            }
        }

        public static string LoadLog()
        {
            if (File.Exists(fileName))
                return File.ReadAllText(fileName);
            else
                return "No log file found";
        }
    }
}
