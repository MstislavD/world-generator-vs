using System;

namespace Utilities
{
    /// <summary>
    /// Interface of an UI element that may modify the parameters of a ParameterList.
    /// </summary>
    public interface IParameterProvider
    {
        /// <summary>
        /// Method implementing creation of an UI control that can update the parameter.
        /// </summary>
        /// <param name="parameter"></param>
        void RegisterParameter(Parameter parameter);
    }

    /// <summary>
    /// A wrapper around a list of parameters.
    /// </summary>
    public class ParameterList
    {
        RandomExt random = new RandomExt();

        List<Parameter> _parameters = new List<Parameter>();
        List<Parameter> _registerdParameters = new List<Parameter>();
        IParameterProvider? _provider;

        public IEnumerable<Parameter> Parameters => _parameters;

        /// <summary>
        /// Give access to list's parameters to a provider.
        /// </summary>
        /// <param name="provider"></param>
        public void RegisterProvider(IParameterProvider provider)
        {
            _provider = provider;
            foreach (Parameter parameter in _registerdParameters)
            {
                _provider.RegisterParameter(parameter);
            }
        }

        /// <summary>
        /// Add a new parameter.
        /// </summary>
        /// <param name="parameter"></param>
        public void Add(Parameter parameter, bool register = true)
        {
            _parameters.Add(parameter);
            if (register)
            {
                _registerdParameters.Add(parameter);
                _provider?.RegisterParameter(parameter);
            }               
        }

        /// <summary>
        /// Check if the list conatins the parameter.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool Contains(Parameter parameter) => _parameters.Contains(parameter);

        /// <summary>Rolls a fresh random seed.</summary>
        public void RegenerateSeeds()
        {
            foreach (Parameter parameter in _parameters)
            {
                if (parameter.GetType() == typeof(ParameterSeed))
                {
                    ((ParameterSeed)parameter).Update(this, random.Next());
                }
            }
        }

        /// <summary>Sets the main seed; per-stage sub-seeds (if any) will be derived from it.</summary>
        public void RegenerateSeeds(int seed)
        {
            random = new RandomExt(seed);
            RegenerateSeeds();
        }
    }
}
