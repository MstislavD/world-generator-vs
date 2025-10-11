namespace Utilities
{
    public interface IParameterProvider
    {
        void RegisterParameter(Parameter parameter);
    }

    public class ParameterList
    {
        List<Parameter> _parameters = new List<Parameter>();

        IParameterProvider? _provider;

        public IEnumerable<Parameter> Parameters => _parameters;

        public void RegisterProvider(IParameterProvider provider)
        {
            _provider = provider;
            foreach (Parameter parameter in _parameters)
            {
                _provider.RegisterParameter(parameter);
            }
        }

        public void Add(Parameter parameter)
        {
            _parameters.Add(parameter);
            parameter.OnUpdate += sender => OnSetUpdate?.Invoke(sender);
            if(_provider != null)
            {
                _provider.RegisterParameter(parameter);
            }
        }

        public bool Contains(Parameter parameter) => _parameters.Contains(parameter);

        public event ParameterUpdateHandler OnSetUpdate = delegate { };
    }
}
