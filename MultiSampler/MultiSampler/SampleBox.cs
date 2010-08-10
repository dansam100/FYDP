using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiSampler
{
    public delegate void AverageAcquiredHandler(double[] values, double output);

    public class SampleBox
    {
        private Stack<double> contents;

        public int AveragingDepth { get; set; }
        public int Size { get; set; }
        public event AverageAcquiredHandler OnAverageAcquired;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="size">size of samples to collect; should be at least depth+1</param>
        /// <param name="depth">how deep should the averaging be?</param>
        public SampleBox(int size, int depth)
        {
            this.AveragingDepth = depth;
            if (size >= depth + 1)
            {
                this.Size = size;
                contents = new Stack<double>(Size);
            }
            else throw new Exception("Size must be greater than depth!");
        }

        public void Add(double sample)
        {
            contents.Push(sample);
        }

        /// <summary>
        /// Perform an average of the current values in the samplebox
        /// </summary>
        /// <param name="indices"></param>
        /// <returns></returns>
        internal double[] PerformAverage(params int[] indices)
        {
            double[] results;
            if (indices.Length > 0)
            {
                results = new double[indices.Length];
                int j = 0;
                double[] array = contents.ToArray<double>();
                foreach(int i in indices)
                {
                    int which = (i % 2 != 0) ? -1 : 1;
                    if( (i+which) < array.Length){
                        results[j] = 0.5*(array[i] + array(i+which));
                    }
                }
                return results;
            }
            else
            {
                results = new double[this.Size];
                double[] array = contents.ToArray<double>();
                int j = 0;
                foreach (int i in this.contents)
                {
                    if (i < array.Length - 1)
                    {
                        results[j] = 0.5 * (contents.Select<double>(i) + contents.Select<double>(i + which));
                    }
                }
            }
        }
    }
}
