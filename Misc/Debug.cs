using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCSharpClient
{
    public class Debug
    {

        public static String GetTime()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        public static void Info(String msg)
        {
            Console.WriteLine(GetTime() + " INFO: " + msg);
        }

        public static void Info(Exception e)
        {
            Console.WriteLine(GetTime() + " INFO: " + e.Message);
        }

        public static void Warning(String msg)
        {
            Console.WriteLine(GetTime() + " WARN: " + msg);
        }

        public static void Warning(Exception e)
        {
            Console.WriteLine(GetTime() + " WARN: " + e.Message);
        }

        public static void Severe(String msg)
        {
            Console.WriteLine(GetTime() + " SEVERE: " + msg);
        }

        public static void Severe(Exception e)
        {
            Console.WriteLine(GetTime() + " SEVERE: " + e);
        }

    }
}
