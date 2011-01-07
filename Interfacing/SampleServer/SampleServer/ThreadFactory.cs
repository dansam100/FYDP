using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SampleServer
{
    public class ThreadFactory
    {
        public int PoolSize { get; set; }
        public int ThreadCount { get; set; }

        private Dictionary<string, Thread> Threads { get; set; }
        private const string THREADNAME = "ClientThread";

        public ThreadFactory(int size)
        {
            this.PoolSize = size;
            Threads = new Dictionary<string,Thread>(size);
        }

        public Thread CreateThread(ThreadMethod target)
        {
            if (this.ThreadCount < this.PoolSize)
            {
                Thread clientThread = new Thread(new ParameterizedThreadStart(target));
                string threadname = string.Format("{0}{1}", THREADNAME, ThreadCount++);
                clientThread.IsBackground = true;

                if (!Threads.ContainsKey(threadname))
                    Threads[threadname] = clientThread;
                else Threads.Add(threadname, clientThread);

                return clientThread;
            }
            else throw new ThreadOverflowException("Error: Attempting to add more client threads than possible");
        }

        public void KillThread(string threadname)
        {
            try
            {
                var thread = this.Threads.Where( item => item.Value.Name.CompareTo(threadname) == 0 ).First();
                Console.Write("Attempting to kill thread {0}...", threadname);
                string key = thread.Key;
                if (thread.Value.IsAlive)
                {
                    //Aborting unused thread
                    thread.Value.Abort();                    
                }
                if (Threads.Keys.Contains(thread.Key))
                {
                    Threads.Remove(thread.Key);
                }
                Console.WriteLine("Killed!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: Failure killing thread {0}", threadname);
            }
        }
    }


    public delegate void ThreadMethod(object o);


    public class ThreadOverflowException : Exception
    {
        public ThreadOverflowException(string exception) : base(exception) { }
    }
}
