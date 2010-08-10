using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MultiSampler
{
    public class Sampler
    {
        public BackgroundWorker worker;
        private TaskItem task;

        public Sampler(TaskItem item)
        {
            this.task = item;

            //runner
            this.worker = new BackgroundWorker();
            this.worker.WorkerSupportsCancellation = true;
            this.worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            this.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }


        public void Start()
        {
            if (!this.worker.IsBusy)
            {
                this.worker.RunWorkerAsync();
            }
        }


        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            if (bw != null)
            {
                this.task.DoWork(sender as BackgroundWorker);
                //this.task.Test(sender as BackgroundWorker);
                if (bw.CancellationPending){
                    e.Cancel = true;
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled){
                string msg = "Task was cancelled: {0}";
                Console.WriteLine(msg);
            }
            else if (e.Error != null){
                // There was an error during the operation.
                string msg = String.Format("An error occurred: {0}", e.Error);
                Console.WriteLine(msg);
            }
            else{
                // The operation completed normally.
                string msg = String.Format("Result = {0}", e.Result);
                Console.WriteLine(msg);
            }
        }

        public void Stop()
        {
            if (!this.worker.IsBusy){
                this.worker.CancelAsync();
            }
        }
    }
}
