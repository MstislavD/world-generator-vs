using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public enum ParameterType { Double, Int, Bool, String }

    public class Parameter
    {
        List<string>? _possibleValues;
        
        public string Name { get; }

        public bool IsSeed { get; } = false;
        public ParameterType Type { get; }
        public ParameterValue Default { get; }
        public ParameterValue Min { get; }
        public ParameterValue Max { get; }
        public ParameterValue Current { get; private set; }
        public  IEnumerable<string>? PossibleValues => _possibleValues;

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

        public Parameter(string name, int defaultValue, int min, int max, bool isSeed = false)
        {
            Name = name;
            Type = ParameterType.Int;
            Default = new ParameterValue(defaultValue);
            Min = new ParameterValue(min);
            Max = new ParameterValue(max);
            Current = new ParameterValue(defaultValue);
            IsSeed = isSeed;
        }

        public Parameter(string name, int defaultValue, bool isSeed = true) : this(name, defaultValue, int.MinValue, int.MaxValue, isSeed: isSeed) { }

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

    public delegate void ParamterUpdateHandler(object sender);

    public abstract class ParameterB
    {
        public string Name { get; }

        public ParameterB(string name)
        {
            Name = name;
        }

        public event ParamterUpdateHandler OnUpdate = delegate { };

        public void Update(object sender, object value)
        {
            if (SetValue(value))
                OnUpdate?.Invoke(sender);
        }

        protected abstract bool SetValue(object value);
    }

    public class Parameter<T> : ParameterB
    {
        public T Default { get; }
        public T Current { get; private set; }
        public Parameter(string name, T defaultValue) : base(name)
        {
            Current = Default = defaultValue;
        }

        protected override bool SetValue(object value)
        {
            if (Current != null && Current.Equals((T)value))
            {
                return false;
            }
            else
            {
                Current = (T)value;
                return true;
            }
        }
    }

    public class ParameterRange<T> : Parameter<T>
    {
        public T Min { get; }
        public T Max { get; }
        public ParameterRange(string name, T defaultValue, T min, T max) : base(name, defaultValue)
        {
            Min = min;
            Max = max;
        }
    }

    public class ParameterSeed : ParameterRange<int>
    {
        public ParameterSeed(string name, int defaultValue) : base(name, defaultValue, int.MinValue, int.MaxValue) { }
    }

    public interface IParameterArray
    {
        IEnumerable<object> PossibleValues { get; }
        ParameterB Parameter { get; }
        object? Current { get; }
    }

    public class ParameterArray<T> : Parameter<T>, IParameterArray
    {
        List<T> _possibleValues = [];
        public ParameterArray(string name, T defaultValue, IEnumerable<T> possibleValues) : base(name, defaultValue)
        {
            _possibleValues = possibleValues.ToList();
        }

        public IEnumerable<T> PossibleValues => _possibleValues;

        public ParameterB Parameter => this;

        IEnumerable<object> IParameterArray.PossibleValues => _possibleValues.Cast<object>();

        object? IParameterArray.Current => Current;

        protected override bool SetValue(object value)
        {
            if (!_possibleValues.Contains((T)value)) throw new Exception();
            return base.SetValue(value);
        }
    }

    public class ParameterEnum<T> : ParameterArray<T>
        where T: Enum
    {
        public ParameterEnum(string name, T defaultValue) : base(name, defaultValue, Enum.GetValues(typeof(T)).Cast<T>().ToList()) { }
    }
}
