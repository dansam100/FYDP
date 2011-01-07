using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiSampler
{
    public delegate void AverageAcquiredHandler(double[] output);

    public class SampleBox
    {
        private Stack<double> contents;

        public int AveragingDepth { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public bool EnableAveraging { get; set; }
        public double[][] Depth;
        public event AverageAcquiredHandler OnAverageAcquired;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="size">size of samples to collect; should be at least depth+1</param>
        /// <param name="depth">how deep should the averaging be?</param>
        public SampleBox(int size, int depth)
        {
            this.AveragingDepth = depth;
            this.EnableAveraging = true;
            if (size >= depth + 1)
            {
                this.Size = size;
                this.Count = 0;
                contents = new Stack<double>(Size);
                Depth = new double[depth+1][];
            }
            else throw new Exception("Size must be greater than depth!");
        }

        public void Add(double sample)
        {
            lock (this)
            {
                contents.Push(sample);
                if (Count < Size) { Count++; }
                this.PerformAveraging();
                //this.Regress();
            }
        }

        /// <summary>
        /// Perform an average of the current values in the samplebox to linearize the system
        /// </summary>
        internal void PerformAveraging()
        {
            int i = 1;
            Depth[0] = (double[])contents.ToArray(Count);
            while (i <= AveragingDepth)
            {
                Depth[i] = Depth[i - 1].Linearize();
                i++;
            }
            if (this.OnAverageAcquired != null && EnableAveraging)
                this.OnAverageAcquired(Depth[i - 1]);
            else
                this.OnAverageAcquired(Depth[0]);
        }


        /// <summary>
        /// Use regression to linearize the system
        /// </summary>
        internal void Regress()
        {
            double yAvg = 0, xAvg = 0;
            double[] values = (double[])contents.ToArray(Count);
            IEnumerable<int> xValues = (IEnumerable<int>)Enumerable.Range(0, values.Length);

            yAvg = values.Average();
            xAvg = xValues.Average();

            double v1 = 0, v2 = 0;

            for (int x = 0; x < values.Length; x++)
            {
                v1 += (x - xAvg) * (values[x] - yAvg);
                v2 += Math.Pow(x - xAvg, 2);
            }

            double a = v1 / v2;
            double b = yAvg - a * xAvg;
            Depth[0] = new double[values.Length];
            for (int x = 0; x < values.Length; x++)
            {
                Depth[0][x] = a * x + b;
            }
            this.OnAverageAcquired(Depth[0]);
        }

        public override string ToString()
        {
            string result = string.Empty;
            for (int i = 0; i <= AveragingDepth; i++)
            {
                //result += ' '.Span(i*3)+Depth[i].ToString(true);
                result += Depth[i].ToString(true);
            }
             
            return result;
        }
    }
}
