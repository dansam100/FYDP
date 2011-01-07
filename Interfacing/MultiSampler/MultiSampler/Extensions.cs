using System;
using System.Linq;
using System.Collections.Generic;

namespace MultiSampler
{
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
            foreach (double data in array)
            {
                if (data > value) return false;
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