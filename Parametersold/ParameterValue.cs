using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public struct ParameterValue
    {
        public static ParameterValue Default = new ParameterValue();

        public double DoubleValue;
        public int IntValue;
        public bool BoolValue;
        public string StringValue;

        public ParameterValue(double value) : this()
        {
            DoubleValue = value;
        }

        public ParameterValue(int value) : this()
        {
            IntValue = value;
        }

        public ParameterValue(bool value) : this()
        {
            BoolValue = value;
        }

        public ParameterValue(string value) : this()
        {
            StringValue = value;
        }
    }
}
