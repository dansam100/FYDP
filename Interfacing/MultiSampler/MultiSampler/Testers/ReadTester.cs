using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MultiSampler
{
    public class ReadTester : TaskItem
    {
        public ReadTester(string name) : base(name) { }
        
        public override void Test(BackgroundWorker worker)
        {
            try{
                while (true)
                {
                    String input = Console.ReadLine();
                    int data = int.Parse(input);
                    samplebox.Add(data);
                }
            }
            catch(Exception e){
                Console.WriteLine(e);
            }
        }

        public override void DoWork(BackgroundWorker worker)
        {
            try
            {
                while (true)
                {
                    String input = Console.ReadLine();
                    double data = double.Parse(input);
                    samplebox.Add(data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
