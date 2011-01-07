using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace MultiSampler.Readers
{
    public class ButtonReader : TaskItem
    {
        public new const string CHANNEL = "Dev1/ai3";

        public ButtonReader(string name) : base(name) { this.Channel = CHANNEL; }

        public ButtonReader(string name, string channel) : base(name, channel) { }

        
        public override void DoWork(System.ComponentModel.BackgroundWorker worker)
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
                        while (!worker.CancellationPending)
                        {
                            data = reader.ReadSingleSample();
                            angle = 60.5 * data[0] - 150; //Console.Write(string.Format("{0:0.00}\r", angle));
                            samplebox.Add(angle);
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
            base.TriggerReadEvent(output.First());
        }


        public override void Test(System.ComponentModel.BackgroundWorker worker)
        {
            throw new NotImplementedException();
        }
    }
}
