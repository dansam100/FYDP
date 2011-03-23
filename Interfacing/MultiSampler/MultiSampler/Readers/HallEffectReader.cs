using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.ComponentModel;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MultiSampler
{
    public enum State { NorthState, SouthState }
    public enum Direction { Forward = 1, Backward = -1, None = 0 }

    public class HallEffectReader : TaskItem
    {
        //NOTE: Change the 'samplebox' averaging 

        const bool FORWARD_ONLY = true;

        /// <summary>
        /// We need this for current readings
        /// </summary>
        public const int SHUNT_RESISTANCE = 249;
        public const double REFERENCE_VALUE = 0.0001d;              //external shunt resistance
        public const double CIRCUMFERENCE_WHEEL = 2.027d;           //wheel radius in meters
        public const int NUM_MAGNET_PAIRS = 3;
        public const double SPEED_THRESHOLD = 1.5;
        public const double DEVIATION_MULTIPLIER = 1.5;
        public const double DEVIATION_OFFSET = 1.0;
        public const int TIMER_INTERVAL = 100;
        public const int INACTIVITY_THRESHOLD = 4 * TIMER_INTERVAL;
        
        public const string CHANNEL = "Dev1/ai2";

        static Stopwatch stopwatch;

        int trigger = 1;

        static long lastUpdate;


        /// <summary>
        /// Metrics table to keep track of read times.
        /// </summary>
        public static Dictionary<State, long> Metrics = new Dictionary<State, long>
        {
            {State.NorthState, 0L},
            {State.SouthState, 0L}
        };

        public State PreviousState { get; set; }

        //TODO: Remove speed and direction if samplebox is enough.
        public double CurrentSpeed { get; set; }
        public Direction Direction { get; set; }

        #region Constructors...
        /// <summary>
        /// static constructor
        /// </summary>
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

            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
        }

        public HallEffectReader(string name, string channel)
            : base(name, channel)
        {
            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
        }
        public HallEffectReader(string name, string channel, string targetIP, int port)
            : base(name, channel, targetIP, port)
        {
            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
        }
        public HallEffectReader(string name, string targetIP, int port)
            : base(name, CHANNEL, targetIP, port)
        {
            updateTimer.Interval = TIMER_INTERVAL;
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
            Direction = Direction.None;
        }
        #endregion

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
        public State GetState(double data)
        {
            if (data > REFERENCE_VALUE)
            {
                return State.NorthState;
            }
            else return State.SouthState;
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
                    if (FORWARD_ONLY)
                    {
                        this.StartTriggerWatcher();
                    }

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

                        //initialize loop parameters and start timers.
                        double[] previousData = null;
                        stopwatch.Start();
                        updateTimer.Start();

                        //keep reading
                        while (!worker.CancellationPending)
                        {
                            //double[] data = reader.ReadSingleSample();
                            double[] data = DoRead(reader);
                            
                            //Console.Write("\r({0})", data[0]);

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
                                    //put in the elapsed time in seconds.
                                    Metrics[PreviousState] = (stopwatch.ElapsedMilliseconds - lastUpdate);

                                    //ensure that no anomalies in read times b/n north and south pulses occur.
                                    if (Direction == Direction.Forward && currentState == State.SouthState)
                                    {
                                        if (Metrics[State.SouthState] > Metrics[State.NorthState])
                                        {
                                            Metrics[State.SouthState] = Metrics[State.NorthState] - 1;
                                        }
                                    }
                                    else if (Direction == Direction.Backward && currentState == State.NorthState)
                                    {
                                        if (Metrics[State.NorthState] > Metrics[State.SouthState])
                                        {
                                            Metrics[State.NorthState] = Metrics[State.SouthState] - 1;
                                        }
                                    }

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
                                samplebox.Add(FORWARD_ONLY ? CurrentSpeed : CurrentSpeed);
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

        private void StartTriggerWatcher()
        {
            ThreadStart start = new ThreadStart(WatchTrigger);
            ExtraThread = new Thread(start);
            ExtraThread.IsBackground = true;
            ExtraThread.Start();
        }

        public void WatchTrigger()
        {
            //Create a virtual channel
            using (Task dirTask = new Task())
            {
                dirTask.AIChannels.CreateVoltageChannel("Dev1/ai3", "TriggerRead",
                                AITerminalConfiguration.Rse, Convert.ToDouble(0),
                                    Convert.ToDouble(5), AIVoltageUnits.Volts);
                //initialize the reader
                AnalogMultiChannelReader reader = new AnalogMultiChannelReader(dirTask.Stream);

                //Verify the Task
                dirTask.Control(TaskAction.Verify);

                bool signal = false;

                while (true)
                {
                    signal = (Math.Round(DoRead(reader).First(), 2) > 0);
                    //Console.WriteLine("Shit is:" + DoRead(reader).First());
                    trigger = (signal) ? -1 : 1;
                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Calculate the new velocity of the system
        /// </summary>
        private void CalculateVelocity()
        {
            long timeSum = (Metrics[State.NorthState] + Metrics[State.SouthState]);
            
            Direction dir = (Metrics[State.NorthState] >= Metrics[State.SouthState]) ? 
                Direction.Forward : Direction.Backward;

            CurrentSpeed = (CIRCUMFERENCE_WHEEL * 1000) / (NUM_MAGNET_PAIRS * timeSum);

            //NOTE: no longer add values when we suspect that their directions went crazy
            //for a value to not be 'crazy', it must have reduced to at least 70% of the original value
            //and yet still be above '0'.
            if (FORWARD_ONLY)
            {
                samplebox.Add(CurrentSpeed);
            }

            else if (Direction == Direction.None || 
                !( (CurrentSpeed >= (0.7 * samplebox.CurrentAverage) && CurrentSpeed >= 0.5) && dir != Direction))
            {
                Direction = dir;

                //add to the samplebox
                samplebox.Add(FORWARD_ONLY ? trigger*CurrentSpeed : (int)(Direction) * CurrentSpeed);
            }
        }

        /// <summary>
        /// Send the current speed every interval.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //base.TriggerReadEvent((int)(Direction) * CurrentSpeed);
            base.TriggerReadEvent(/*CurrentSpeed == 0.0d ? 0 : */trigger * samplebox.CurrentAverage);
            Console.Clear();
            Console.Write("Sending: {0:0.##}\n", trigger * samplebox.CurrentAverage);
            //Console.Write("Meanwhile: {0:0.##}\n", (int)(Direction) * CurrentSpeed);
            //Console.Write("Sending: {0:0.0000}\r", (int)(Direction) * CurrentSpeed);
        }
    }
}
