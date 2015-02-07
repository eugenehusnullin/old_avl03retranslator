using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepairFromLog
{
    class Program
    {
        static void Main(string[] args)
        {
            Processing processing = new Processing();
            Console.Out.WriteLine(DateTime.Now.ToString() + " --- " + "Path: " + args[0]);
            Console.Out.WriteLine(DateTime.Now.ToString() + " --- " + "Host: " + args[1]);
            Console.Out.WriteLine(DateTime.Now.ToString() + " --- " + "Port: " + args[2]);
            processing.start(args[0], args[1], Convert.ToInt32(args[2]));
        }
    }
}
