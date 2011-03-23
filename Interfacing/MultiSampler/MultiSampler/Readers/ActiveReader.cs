using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using NationalInstruments.DAQmx;
using System.ComponentModel;
using System.Threading;

namespace MultiSampler.Readers
{
    public class ActiveReader : TaskItem
    {
        public const string CHANNEL = "Dev1/ai3";
        
        public ActiveReader(string name) : base(name) { this.Channel = CHANNEL; }
        public ActiveReader(string name, string channel) : base(name, channel) { }
        public ActiveReader(string name, string targetIP, int port) : base(name, CHANNEL, targetIP, port) { }

        private Timer updateTimer;
        private double signal;

        public override void DoWork(BackgroundWorker worker)
        {
            updateTimer = new Timer();
            updateTimer.Interval = 100;
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(UpdateTimer_Tick);
            updateTimer.Start();

            while (!worker.CancellationPending)
            {
                try
                {
                    //this.Connect();

                    System.Console.WriteLine("Initializing DAQ stuff");
                    using (Task myTask = new Task())
                    {
                        //Create a virtual channel
                        myTask.AIChannels.CreateVoltageChannel(Channel, Name,
                            AITerminalConfiguration.Rse, Convert.ToDouble(0),
                                Convert.ToDouble(5), AIVoltageUnits.Volts);
                        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(myTask.Stream);
                        //Verify the Task
                        myTask.Control(TaskAction.Verify);

                        //completed initialization.
                        //Now to read some stuff
                        System.Console.WriteLine("Initialized DAQ stuff");

                        double[] data;
                        while (!worker.CancellationPending)
                        {
                            data = DoRead(reader);
                            signal = Math.Round(data.First(), 2);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Read Failed.\nReason: {1}", Name, e);
                }
            }
        }


        public void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Console.Write("Signal: {0:0.00}\r", signal);
           
            //base.TriggerReadEvent(signal);
        }
    }
}
