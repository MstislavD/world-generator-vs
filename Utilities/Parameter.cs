using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    /// <summary>
    /// Base class for a parameter, an object containing a value, an update method, and an event that is raised when the value is updated.
    /// </summary>
    public abstract class Parameter
    {
        /// <summary>
        /// The name of the parameter to be displayed by UI.
        /// </summary>
        public string Name { get; }

        public Parameter(string name) { Name = name; }

        /// <summary>
        /// Occurs when the parameter's value is updated.
        /// </summary>
        public event EventHandler OnUpdate = delegate { };

        /// <summary>
        /// Update the value of the parameter.
        /// </summary>
        /// <param name="sender">The sender of the update command.</param>
        /// <param name="value">New value of the parameter.</param>
        public void Update(object sender, object value)
        {
            if (SetValue(value))
                OnUpdate?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Setting the value of the parameter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract bool SetValue(object value);
    }

    /// <summary>
    /// Parameter with value of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Parameter<T> : Parameter
    {
        public static implicit operator T(Parameter<T> p) => p.Current;
        public static bool operator ==(Parameter<T> p, T? v) => p.Current == null ? v == null : p.Current.Equals(v);
        public static bool operator !=(Parameter<T> p, T? v) => p.Current == null ? v != null : !p.Current.Equals(v);
        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Current value of the parameter.
        /// </summary>
        public T Current { get; private set; }
        public Parameter(string name, T defaultValue) : base(name) { Current = defaultValue; }

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

    /// <summary>
    /// Parameter of a type with min and max values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ParameterRange<T> : Parameter<T>
        where T : IComparisonOperators<T, T, bool>
    {
        public T Min { get; }
        public T Max { get; }
        public ParameterRange(string name, T defaultValue, T min, T max) : base(name, defaultValue)
        {
            if (max < min)
                throw new ArgumentException();
            Min = min;
            Max = max;
        }

        protected override bool SetValue(object value)
        {
            if ((T)value < Min || (T)value > Max) 
                throw new ArgumentException();
            return base.SetValue(value);
        }
    }

    /// <summary>
    /// An integer parameter representing a seed for RNG.
    /// </summary>
    public class ParameterSeed : Parameter<int>
    {
        public ParameterSeed(string name, int defaultValue) : base(name, defaultValue) { }
    }

    /// <summary>
    /// A parameter with fixed number of possible values.
    /// </summary>
    public class ParameterArray : Parameter<object>
    {
        List<object> _possibleValues = [];
        public ParameterArray(string name, object defaultValue, IEnumerable<object> possibleValues) : base(name, defaultValue)
        {
            _possibleValues = possibleValues.ToList();
        }

        /// <summary>
        /// Returns all possible values of the parameter.
        /// </summary>
        public IEnumerable<object> PossibleValues => _possibleValues;

        protected override bool SetValue(object value)
        {
            if (!_possibleValues.Contains(value)) 
                throw new Exception();
            return base.SetValue(value);
        }
    }

    /// <summary>
    /// A special case of the ParameterArray with enum as input.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ParameterEnum<T> : ParameterArray
        where T: Enum
    {
        public ParameterEnum(string name, T defaultValue) : base(name, defaultValue, Enum.GetValues(typeof(T)).Cast<object>().ToList()) { }
    }
}
