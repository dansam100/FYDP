using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Timers;

namespace MultiSampler
{
    public enum State { NorthState, SouthState }
    public enum Direction { Forward = 1, Backward = -1 }
    
    public class HallEffectReader : TaskItem
    {
        /// <summary>
        /// We need this for current readings
        /// </summary>
        public const int SHUNT_RESISTANCE = 249;
        public const double REFERENCE_VALUE = 0.0001d;              //external shunt resistance
        public const double CIRCUMFERENCE_WHEEL = 2.027d;         //wheel radius in meters
        public const int NUM_MAGNET_PAIRS = 2;

        private const int TIMER_INTERVAL = 100;

        public const string CHANNEL = "Dev1/ai1";

        public const int INACTIVITY_THRESHOLD = 1000;

        static Stopwatch stopwatch;
        private Timer updateTimer;

        static long lastUpdate;

        public static Dictionary<State, long> Metrics = new Dictionary<State, long>
        {
            {State.NorthState, 0L},
            {State.SouthState, 0L}
        };

        public State PreviousState { get; set; }

        //TODO: Remove speed and direction if samplebox is enough.
        public double CurrentSpeed { get; set; }
        public Direction Direction { get; set; }
        
        static HallEffectReader()
        {
            stopwatch = new Stopwatch();
            lastUpdate = 0;
        }

        /// <summary>
        /// Ctors
        /// </summary>
        /// <param name="name"></param>
        public HallEffectReader(string name)
            : base(name)
        {
            this.Channel = CHANNEL;

            updateTimer = new Timer();
            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new ElapsedEventHandler(updateTimer_Elapsed);
        }

        public HallEffectReader(string name, string channel)
            : base(name, channel)
        {
            updateTimer = new Timer();
            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new ElapsedEventHandler(updateTimer_Elapsed);
        }
        public HallEffectReader(string name, string channel, string targetIP, int port)
            : base(name, channel, targetIP, port)
        {
            updateTimer = new Timer();
            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new ElapsedEventHandler(updateTimer_Elapsed);
        }

        public void SetupClock()
        {
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
            if (stopwatch.ElapsedMilliseconds > 0)
            {
                stopwatch.Reset();
            }
        }


        public override void DoWork(BackgroundWorker worker)
        {           
            while (!worker.CancellationPending)
            {
                try
                {
                    //connect to the server first.
                    this.Connect();
                    this.SetupClock();

                    using (myTask = new Task())
                    {
                        //Create a virtual channel
                        myTask.AIChannels.CreateCurrentChannel(Channel, Name,
                            AITerminalConfiguration.Rse, Convert.ToDouble(-0.004),
                                Convert.ToDouble(0.004), SHUNT_RESISTANCE, AICurrentUnits.Amps);
                        
                        //initialize the reader
                        AnalogMultiChannelReader reader = new AnalogMultiChannelReader(myTask.Stream);

                        //Verify the Task
                        myTask.Control(TaskAction.Verify);

                        double[] previousData = null;

                        //keep reading
                        while (!worker.CancellationPending)
                        {
                            double[] data = reader.ReadSingleSample();
                            Console.Write("\r({0})", data[0]);

                            //TODO: Add computations for direction.             SUCKSEED
                            //TODO: Find a way to check for zero-speed.         SUCKSEED
                            //TODO: Fix the interval vs. absolute time issue.   SUCKSEED

                            //check if sample is different.
                            if (!data.InSampleWindow(previousData))
                            {
                                //get the current time and store the elapsed stuff.
                                State currentState = GetState(data[0]);
                                if (currentState != PreviousState)
                                {
                                    Metrics[PreviousState] = stopwatch.ElapsedMilliseconds - lastUpdate;
                                    lastUpdate = stopwatch.ElapsedMilliseconds;
                                    PreviousState = currentState;

                                    //calculate the direction and speed.
                                    CalculateVelocity();
                                }

                                //assign reference values
                                previousData = data;
                            }
                            else if ((stopwatch.ElapsedMilliseconds - lastUpdate) > INACTIVITY_THRESHOLD)
                            {
                                //when inactivity time is exceeded, set it to zero.
                                CurrentSpeed = 0d;
                                lastUpdate = stopwatch.ElapsedMilliseconds;

                                //add to the samplebox
                                samplebox.Add(CurrentSpeed);
                            }
                        }
                    }
                }
                catch (DaqException e){
                    Console.WriteLine(e.Message);
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
        }

        private void CalculateVelocity()
        {
            long timeSum = Metrics[State.NorthState] + Metrics[State.SouthState];
            CurrentSpeed = CIRCUMFERENCE_WHEEL / (NUM_MAGNET_PAIRS * timeSum);
            Direction = (Metrics[State.NorthState] >= Metrics[State.SouthState]) ? Direction.Forward : Direction.Backward;

            //add to the samplebox
            samplebox.Add((int)(Direction) * CurrentSpeed);
        }

        public State GetState(double data)
        {
            if (data > REFERENCE_VALUE)
            {
                return State.NorthState;
            }
            else return State.SouthState;
        }


        protected override void SampleBox_DataAcquired(double[] output)
        {
            //base.TriggerReadEvent(output.First());
            //System.Console.Write("\r{0:0.00}", output.First());
        }

        /// <summary>
        /// Send the current speed every interval.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void updateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //base.TriggerReadEvent((int)(Direction) * CurrentSpeed);
            base.TriggerReadEvent(samplebox.CurrentAverage);
        }
    }
}
