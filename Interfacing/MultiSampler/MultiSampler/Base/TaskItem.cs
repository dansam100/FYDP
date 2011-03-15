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
        public string Channel { get; set; }

        protected Task myTask;
        protected TcpClient connection;
        protected NetworkStream stream;
        
        protected const string targetIP = "127.0.0.1";
        protected const int port = 9191;

        protected SampleBox samplebox;

        public event DataReadEventHandler OnEventRead;
        public event CharReadEventHandler OnCharRead;

        public TaskItem(string name)
        {
            this.Name = name;
            this.connection = new TcpClient();

            this.samplebox = new SampleBox(5, 3);

            //TODO: this is for testing. remove later.
            //this.samplebox.OnAverageAcquired += new AverageAcquiredHandler(samplebox_OnAverageAcquired);

            //TODO: actual functionality. re-enable
            this.samplebox.OnAverageAcquired += new AverageAcquiredHandler(SampleBox_DataAcquired);

            this.OnEventRead += new DataReadEventHandler(TaskItem_OnEventRead);
            this.OnCharRead += new CharReadEventHandler(TaskItem_OnCharRead);
        }

        public TaskItem(string name, string channel)  : this(name)
        {
            this.Channel = channel;
        }

        public abstract void DoWork(BackgroundWorker worker);
        public abstract void Test(BackgroundWorker worker);

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
                    connection.Connect(targetIP, port);
                    stream = connection.GetStream();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to connect to server.\nReason: {0}", e);
                }
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
                        catch (Exception e){ Console.WriteLine(e);   
                    }
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


        protected abstract void SampleBox_DataAcquired(double[] output);
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
}
