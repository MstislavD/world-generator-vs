using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public delegate void ParameterUpdateHandler(object sender);

    public abstract class Parameter
    {
        public string Name { get; }

        public Parameter(string name)
        {
            Name = name;
        }

        public event ParameterUpdateHandler OnUpdate = delegate { };

        public void Update(object sender, object value)
        {
            if (SetValue(value))
                OnUpdate?.Invoke(sender);
        }

        protected abstract bool SetValue(object value);
    }

    public class Parameter<T> : Parameter
    {
        public static implicit operator T(Parameter<T> p) => p.Current;

        public static bool operator ==(Parameter<T> p, T v) => p.Current.Equals(v);
        public static bool operator !=(Parameter<T> p, T v) => !p.Current.Equals(v);

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

    public class ParameterArray : Parameter<object>
    {
        List<object> _possibleValues = [];
        public ParameterArray(string name, object defaultValue, IEnumerable<object> possibleValues) : base(name, defaultValue)
        {
            _possibleValues = possibleValues.ToList();
        }

        public IEnumerable<object> PossibleValues => _possibleValues;

        protected override bool SetValue(object value)
        {
            if (!_possibleValues.Contains(value)) throw new Exception();
            return base.SetValue(value);
        }
    }

    public class ParameterEnum<T> : ParameterArray
        where T: Enum
    {
        public ParameterEnum(string name, T defaultValue) : base(name, defaultValue, Enum.GetValues(typeof(T)).Cast<object>().ToList()) { }
    }
}
