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
        public const string DEFAULT_IP = "127.0.0.1";
        public const int DEFAULT_PORT = 9191;
        
        public string Name{ get; set; }
        public string Channel { get; set; }

        protected Task myTask;
        protected TcpClient connection;
        protected NetworkStream stream;
        
        protected string TargetIP { get; set; }
        protected int Port { get; set; }

        protected SampleBox samplebox;

        public event DataReadEventHandler OnEventRead;
        public event CharReadEventHandler OnCharRead;

        public TaskItem(string name)
        {
            this.TargetIP = DEFAULT_IP;
            this.Port = DEFAULT_PORT;
            this.Name = name;

            //tcp connection client
            this.connection = new TcpClient();

            this.samplebox = new SampleBox(5, 3);

            //get the sampled values. this can be left to the derived classes.
            this.samplebox.OnAverageAcquired += new AverageAcquiredHandler(SampleBox_DataAcquired);

            this.OnEventRead += new DataReadEventHandler(TaskItem_OnEventRead);
            this.OnCharRead += new CharReadEventHandler(TaskItem_OnEventCharRead);
        }

        public TaskItem(string name, string channel)  : this(name)
        {
            this.Channel = channel;
        }

        public TaskItem(string name, string channel, string targetIP, int port)
            : this(name, channel)
        {
            this.TargetIP = targetIP;
            this.Port = port;
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        protected void Connect()
        {
            while (!this.connection.Connected)
            {
                try
                {
                    if (this.connection.Connected)
                    {
                        this.connection.Close();
                        this.connection = new TcpClient();
                    }
                    connection.Connect(TargetIP, Port);
                    stream = connection.GetStream();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to connect to server.\nReason: {0}", e);
                }
            }
        }

        /// <summary>
        /// Do work
        /// </summary>
        /// <param name="worker"></param>
        public abstract void DoWork(BackgroundWorker worker);

        /// <summary>
        /// For testing purposes
        /// </summary>
        /// <param name="worker"></param>
        public virtual void Test(BackgroundWorker worker)
        {
            ConsoleKeyInfo key;
            try
            {
                this.Connect();
                ASCIIEncoding ascii = new ASCIIEncoding();
                byte[] data = new byte[64];
                while (true)
                {
                    key = Console.ReadKey(true);
                    data = ascii.GetBytes(new char[] { key.KeyChar });
                    TriggerReadEvent(data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// Read handlers
        /// </summary>
        /// <param name="data"></param>
        protected virtual void TaskItem_OnEventRead(double data)
        {
            try
            {
                if (connection.Connected){
                    try
                    {
                        byte[] dataBytes = BitConverter.GetBytes(Math.Round(data, 2));
                        stream.Write(dataBytes, 0, dataBytes.Length);
                        stream.Flush();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else Connect();
            }
            catch (Exception e){
                Console.WriteLine("Unable to transmit data to server!\nReason: {0}", e);
            }
        }
        public void TaskItem_OnEventCharRead(byte[] data)
        {
            try
            {
                if (connection.Connected)
                {
                    try
                    {
                        stream.Write(data, 0, data.Length);
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


        protected virtual void SampleBox_DataAcquired(double[] output)
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
}
