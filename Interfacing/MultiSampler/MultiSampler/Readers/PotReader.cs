using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.Net.Sockets;
using System.ComponentModel;
using System.Timers;

namespace MultiSampler
{
    public class PotReader : TaskItem
    {
        public const string CHANNEL = "Dev1/ai1";
        
        public PotReader(string name) : base(name) { this.Channel = CHANNEL; }

        public PotReader(string name, string channel) : base(name, channel) { }

        Timer timer = new Timer();
        bool sendData = false;

        public override void Test(BackgroundWorker worder)
        {
            throw new NotImplementedException();
        }

        public override void DoWork(BackgroundWorker worker)
        {
            timer.Interval = 50;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Tick);
            timer.Start();

            while (!worker.CancellationPending)
            {
                try
                {
                    this.Connect();

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


                        System.Console.WriteLine("Initialized DAQ stuff");

                        double[] data;
                        double angle;
                        while (!worker.CancellationPending)
                        {
                            if (!sendData) continue;

                            data = reader.ReadSingleSample();

                            angle = -(data[0]/(4.4/270) - 270/2); //Console.Write(string.Format("{0:0.00}\r", angle));
                            byte[] dataBytes = BitConverter.GetBytes(Math.Round(angle, 2));
                            System.Console.Write("\rSending {0:0.00}", Math.Round(angle, 2));
                            stream.Write(dataBytes, 0, sizeof(double));
                            sendData = false;
                        }
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine("{0} Read Failed.\nReason: {1}", Name, e);
                }
            }
        }

        protected override void SampleBox_DataAcquired(double[] output)
        {
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            //System.Console.Write("\rPulses in the past second: {0} ", pulses);
            sendData = true;
            timer.Start();
        }
    }
}
