using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public enum ParameterType { Double, Int, Bool, String }

    public class Parameter
    {
        List<string> _possibleValues;
        
        public string Name { get; }
        public ParameterType Type { get; }
        public ParameterValue Default { get; }
        public ParameterValue Min { get; }
        public ParameterValue Max { get; }
        public ParameterValue Current { get; private set; }
        public  IEnumerable<string> PossibleValues => _possibleValues;

        public static implicit operator int(Parameter p) => p.Current.IntValue;
        public static implicit operator double(Parameter p) => p.Current.DoubleValue;
        public static implicit operator bool(Parameter p) => p.Current.BoolValue;
        public static implicit operator string(Parameter p) => p.Current.StringValue;

        public Parameter(string name, double defaultValue, double min, double max)
        {
            Name = name;
            Type = ParameterType.Double;
            Default = new ParameterValue(defaultValue);
            Min = new ParameterValue(min);
            Max = new ParameterValue(max);
            Current = new ParameterValue(defaultValue);
        }

        public Parameter(string name, int defaultValue, int min, int max)
        {
            Name = name;
            Type = ParameterType.Int;
            Default = new ParameterValue(defaultValue);
            Min = new ParameterValue(min);
            Max = new ParameterValue(max);
            Current = new ParameterValue(defaultValue);
        }

        public Parameter(string name, int defaultValue) : this(name, defaultValue, int.MinValue, int.MaxValue) { }

        public Parameter(string name, bool defaultValue)
        {
            Name = name;
            Type = ParameterType.Bool;
            Default = new ParameterValue(defaultValue);
            Current = new ParameterValue(defaultValue);
        }

        public Parameter(string name, string defaultValue, IEnumerable<string> possibleValues)
        {
            Name = name;
            Type = ParameterType.String;
            Default = new ParameterValue(defaultValue);
            Current = new ParameterValue(defaultValue);
            _possibleValues = possibleValues.ToList();
        }

        internal void SetValue(ParameterValue value) => Current = value;

    }
}
