using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCClientSharp
{
    public class Debug
    {

        public static void Info(String msg)
        {
            Console.WriteLine("INFO: " + msg);
        }

        public static void Info(Exception e)
        {
            Console.WriteLine("INFO: " + e);
        }

        public static void Warning(String msg)
        {
            Console.WriteLine("INFO: " + msg);
        }

        public static void Warning(Exception e)
        {
            Console.WriteLine("INFO: " + e);
        }

        public static void Severe(String msg)
        {
            Console.WriteLine("INFO: " + msg);
        }

        public static void Severe(Exception e)
        {
            Console.WriteLine("INFO: " + e);
        }

    }
}
