using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using System.Net.Sockets;
using System.ComponentModel;
using System.Collections;

namespace MultiSampler
{
    public delegate void DataReadEventHandler(double data);
    public delegate void CharReadEventHandler(byte[] data);
    
    public abstract class TaskItem
    {
        public string Name{ get; set; }
        protected Task myTask;
        protected TcpClient connection;
        protected NetworkStream stream;
        
        protected const string targetIP = "localhost";
        protected const int port = 9191;

        protected SampleBox samplebox;

        public event DataReadEventHandler OnEventRead;
        public event CharReadEventHandler OnCharRead;

        public TaskItem(string name)
        {
            this.Name = name;
            this.connection = new TcpClient();

            this.samplebox = new SampleBox(5, 3);
            this.samplebox.OnAverageAcquired += new AverageAcquiredHandler(samplebox_OnAverageAcquired);

            this.OnEventRead += new DataReadEventHandler(TaskItem_OnEventRead);
            this.OnCharRead += new CharReadEventHandler(TaskItem_OnCharRead);
        }

        public abstract void DoWork(BackgroundWorker worker);
        public abstract void Test(BackgroundWorker worker);

        protected void Connect()
        {
            try
            {
                if(this.connection.Connected)
                {
                    this.connection.Close();
                }
                connection.Connect(targetIP, port);
                stream = connection.GetStream();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to connect to server.\nReason: {0}", e);
            }
        }

        protected virtual void TaskItem_OnEventRead(double data)
        {
            try
            {
                if (connection.Connected){
                    NetworkStream stream = connection.GetStream();
                    ASCIIEncoding ascii = new ASCIIEncoding();
                    byte[] buff = new byte[128];
                    try
                    {
                        //string outval = string.Format("{0:0.00}\0", data);
                        string outval = string.Format("{0}\0", (int)data);
                        buff = ascii.GetBytes(outval);
                        stream.Write(buff, 0, outval.Length);
                        stream.Flush();
                    }
                    catch (Exception e){    Console.WriteLine(e);   }
                }
                else Connect();
            }
            catch (Exception e){
                Console.WriteLine("Unable to transmit data to server!\nReason: {0}", e);
            }
        }

        public void TaskItem_OnCharRead(byte[] data)
        {
            try
            {
                if (connection.Connected)
                {
                    try
                    {
                        stream.Write(data, 0, 1);
                        stream.Flush();
                    }
                    catch (Exception e){}
                }
                else Connect();
            }
            catch (Exception e)
            {
            }
        }

        void samplebox_OnAverageAcquired(double[] output)
        {
            System.Console.WriteLine("Results:\n");
            System.Console.WriteLine(samplebox);
            //this.TriggerReadEvent(output.FirstOrDefault());
        }

        protected void TriggerReadEvent(double data)
        {
            lock (this)
            {
                if (this.OnEventRead != null)
                    this.OnEventRead(data);
            }
        }

        protected void TriggerReadEvent(byte[] data)
        {
            lock (this)
            {
                if (this.OnCharRead != null)
                    this.OnCharRead(data);
            }
        }
    }


    /// <summary>
    /// Extension methods for checking data validity.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Print an array in some desired format
        /// </summary>
        /// <param name="array">array to print</param>
        /// <returns>Desired format</returns>
        public static string ToFormattedString(this double[] array)
        {
            string result = "";
            foreach (double data in array)
            {
                result += string.Format("{0},", data);
            }
            return result.Substring(0, result.Length - 1);
        }

        /*
        public static bool InSampleWindow(this double[] array, double[] other)
        {
            int i = 0;
            double mask = 1000;
            bool equal = false;
            if (other != null)
            {
                if (array.Length == other.Length)
                {
                    while (i < array.Length)
                    {
                        double a = array[i] * mask;
                        double b = other[i] * mask;
                        
                        equal = ((int)a == (int)b);
                        i++;
                    }
                }
            }
            return equal;
        }
        */


        /// <summary>
        /// Checks if the difference between the contents of 'array' and 'other' are within the same window.
        /// </summary>
        /// <param name="array">array to check</param>
        /// <param name="other">values to check against</param>
        /// <returns>true if they are within the same sample window</returns>
        public static bool InSampleWindow(this double[] array, double[] other)
        {
            if (array != null && other != null)
            {
                if ((Math.Abs(array[0] - other[0])) < 0.001)
                {
                    return true;
                }
            }
            return false;
        }
		
        /// <summary>
        /// Checks if the values inside 'array' are less than that of 'value'
        /// </summary>
        /// <param name="array">the array of values to check</param>
        /// <param name="value">the value to compare against</param>
        /// <returns>true if all elements of 'array' are less than that of 'value'</returns>
		public static bool LessThan(this double[] array, double value)
		{
			foreach(double data in array){
				if(data > value) return false;
			}
			return true;
		}

        /// <summary>
        /// Slice a given stack into a 'count' sized array.
        /// </summary>
        /// <param name="stack">stack containing array of doubles</param>
        /// <param name="count">size to slice stack into</param>
        /// <returns>array of containing 'count' elements from 'stack'</returns>
        public static IEnumerable<double> ToArray(this Stack<double> stack, int count)
        {
            double[] array = stack.ToArray<double>();
            double[] result = new double[count];
            Array.Copy(array, 0, result, 0, count);
            return result;
        }

        /// <summary>
        /// Computes averages of values in the array.
        /// Performs an average of each successive 2 elements
        /// </summary>
        /// <param name="array">array of values to average</param>
        /// <returns>an array of computed averages</returns>
        public static double[] Linearize(this double[] array)
        {
            int size = array.Length - 1;
            if (array.Length > 1)
            {
                double[] results = new double[size];
                for (int i = 0; (i + 1) < array.Length; i++)
                {
                    results[i] = 0.5 * (array[i] + array[i + 1]);
                }
                return results;
            }
            return array;
        }

        /// <summary>
        /// An array tostring override.
        /// </summary>
        /// <param name="array">the array to format</param>
        /// <param name="yes">dummy override parameter</param>
        /// <returns>string of formatted output</returns>
        public static string ToString(this double[] array, bool yes)
        {
            String results = String.Empty;
            for (int i = 0; i < array.Length; i++)
            {
               results += String.Format("{0}{1}", array[i], ' '.Span(6));
            }
            results += "\n";
            return results;
        }

        /// <summary>
        /// Does a multiplier on a character to create a string of length 'count'.
        /// </summary>
        /// <param name="input">the character to expand</param>
        /// <param name="size">the length to expand to</param>
        /// <returns>string containing 'count' characters of 'input'</returns>
        public static string Span(this char input, int size)
        {
            string results = string.Empty;
            for (int i = 0; i < size; i++) { results += input; }
            return results;
        }
    }
}
