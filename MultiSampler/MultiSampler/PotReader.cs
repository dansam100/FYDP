using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.Net.Sockets;
using System.ComponentModel;

namespace MultiSampler
{
    public class PotReader : TaskItem
    {
        public const string CHANNEL = "Dev1/ai1";
        
        public PotReader(string name) : base(name){}

        public override void Test(BackgroundWorker worder)
        {
            throw new NotImplementedException();
        }

        public override void DoWork(BackgroundWorker worker)
        {
            while (!worker.CancellationPending)
            {
                try
                {
                    this.Connect();

                    using (myTask = new Task())
                    {
                        //Create a virtual channel
                        myTask.AIChannels.CreateVoltageChannel(CHANNEL, Name,
                            AITerminalConfiguration.Rse, Convert.ToDouble(0),
                                Convert.ToDouble(5), AIVoltageUnits.Volts);

                        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(myTask.Stream);

                        //Verify the Task
                        myTask.Control(TaskAction.Verify);
                        double[] data;
                        double angle;
                        while(!worker.CancellationPending)
                        {
                            data = reader.ReadSingleSample();
                            angle = 60.5 * data[0] - 150;
                            Console.Write(string.Format("{0:0.00}\r", angle));
                            base.TriggerReadEvent(angle);
                        }
                    }
                }
                catch (Exception e)
                {
                }
            }
        }
    }
}
