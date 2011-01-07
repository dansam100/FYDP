using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Started Server...");
            Server s = new Server();
            s.Start();
        }
    }
}
