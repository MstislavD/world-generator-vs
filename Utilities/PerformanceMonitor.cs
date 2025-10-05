using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    // Experimenting with attributes to measure method performance. Doesn't work currently
    [AttributeUsage(AttributeTargets.Method)]
    public class MeasurePerformanceAttribute: Attribute
    {
        public MeasurePerformanceAttribute() { }
    }

    public class PerformanceMonitor
    {
        public static T Measurement<T>(Func<T> method, bool logToConsole = true)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                return method();
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"{method.Method.Name} completed in {stopwatch.ElapsedMilliseconds} ms");
            }
        }
    }
}
