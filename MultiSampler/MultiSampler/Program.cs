using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiSampler
{
    public class Program
    {
        static void Main(string[] args)
        {
            MultiSampler ms = new MultiSampler();
            bool exit = false;
            Console.WriteLine("Started client system.\nAwaiting input:");
            try
            {
                ms.BeginSampling();
                while (true);
                /*
                while (!exit)
                {
                    string input = Console.ReadLine();
                    switch (input)
                    {
                        case "start":
                            ms.BeginSampling();
                            Console.WriteLine("started");
                            break;
                        case "stop":
                            ms.StopSampling();
                            exit = true;
                            break;
                        default:
                            break;
                    }
                }
                */
            }
            catch (Exception e){
                Console.WriteLine("Error: {0}", e);
            }
        }
    }
}
