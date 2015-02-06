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
            Console.Out.WriteLine("Path: " + args[0]);
            Console.Out.WriteLine("Host: " + args[1]);
            Console.Out.WriteLine("Port: " + args[2]);
            processing.start(args[0], args[1], Convert.ToInt32(args[2]));
        }
    }
}
