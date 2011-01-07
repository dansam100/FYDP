using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;

namespace MultiSampler
{
    public class HallEffectReader : TaskItem
    {
        public const int SHUNT_RESISTANCE = 249;

        public const string CHANNEL = "Dev1/ai2";
		
		public const double REFERENCE_VALUE = 0.0001;   //external shunt resistance
        public const double WHEEL_RADIUS = 0.3;         //wheel radius in meters

        public HallEffectReader(string name) : base(name){}

        public override void Test(BackgroundWorker worker)
        {
            ConsoleKeyInfo key;
            try{
                this.Connect();
                ASCIIEncoding ascii = new ASCIIEncoding();
                byte[] data = new byte[64];
                while (true){
                    key = Console.ReadKey(true);
                    data = ascii.GetBytes(new char[] { key.KeyChar });
                    base.TriggerReadEvent(data);

                    //stream.Write(data, 0, 1);
                    //String input = Console.ReadLine();
                    //data = ascii.GetBytes(input);
                    //stream.Write(data, 0, input.Length);
                    //stream.Flush();
                }
            }
            catch(Exception e){
                Console.WriteLine(e);
            }
        }

        public override void DoWork(BackgroundWorker worker)
        {
            TimeSpan northPulse = new TimeSpan(0),
					 southPulse = new TimeSpan(0);
			DateTime lastPulse  = DateTime.Now;
			
			while (!worker.CancellationPending)
            {
                double[] previous = null;
                try{
                    //this.Connect();
                    using (myTask = new Task())
                    {
                        //Create a virtual channel
                        myTask.AIChannels.CreateCurrentChannel(CHANNEL, Name,
                            AITerminalConfiguration.Rse, Convert.ToDouble(-0.004),
                                Convert.ToDouble(0.004), SHUNT_RESISTANCE, AICurrentUnits.Amps);

                        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(myTask.Stream);

                        //Verify the Task
                        myTask.Control(TaskAction.Verify);

                        //keep reading
                        while (!worker.CancellationPending)
                        {
                            double[] data = reader.ReadSingleSample();

                            //check if sample is different.
                            if (!data.InSampleWindow(previous))
                            {
								if(data.LessThan(REFERENCE_VALUE)) {  
                                    southPulse = DateTime.Now - lastPulse; Console.WriteLine("it's north!");
                                }
								else{ 
                                    northPulse = DateTime.Now - lastPulse;Console.WriteLine("it's south!");
                                }
                                
                                //assign reference values
                                previous = data;
								lastPulse = DateTime.Now;

                                //check round-trip time in ms.
                                TimeSpan elapsed = northPulse + southPulse;
                                //calculate speed in rev/ms
                                double revPms = 1 / (elapsed.Milliseconds);

                                //check direction
								if(northPulse > southPulse){ 
                                    Console.WriteLine("{0}", elapsed.Milliseconds);
                                    this.samplebox.Add(elapsed.Milliseconds/1000);
                                }
								else { 
                                    Console.WriteLine("{0}", -1*elapsed.Milliseconds);
                                    this.samplebox.Add(-1*elapsed.Milliseconds/1000);
                                }
                            }
                        }
                    }
                }
                catch (DaqException e){
                    Console.WriteLine(e.Message);
                }
                catch (Exception e) { }
            }
        }
    }
}
