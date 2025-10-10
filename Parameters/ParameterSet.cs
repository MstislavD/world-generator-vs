using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public abstract class ParameterSet : IParameterSet
    {
        List<Parameter> _parameters = new List<Parameter>();

        public event EventHandler OnSetUpdate;

        public IEnumerable<Parameter> Parameters => _parameters;

        protected ParameterSet(IParameterSupplier supplier)
        {
            supplier.OnParameterUpdate += _updateParameter;
        }

        protected ParameterSet() { }

        private void _updateParameter(object sender, Parameter parameter)
        {
            IParameterSupplier supplier = (IParameterSupplier)sender;
            parameter.SetValue(supplier.GetValue(parameter));
            OnSetUpdate?.Invoke(this, new EventArgs());
        }

        protected void AddParameter(Parameter parameter)
        {
            _parameters.Add(parameter);
        }

        protected void UpdateParameter(Parameter parameter, ParameterValue value)
        {
            parameter.SetValue(value);
            OnSetUpdate?.Invoke(this, new EventArgs());
        }
    }

    public class ParameterList
    {
        List<ParameterB> _parameters = new List<ParameterB>();

        IParameterBSupplier? _supplier;

        public IEnumerable<ParameterB> Parameters => _parameters;

        public void RegisterSupplier(IParameterBSupplier supplier)
        {
            _supplier = supplier;
            foreach (ParameterB parameter in _parameters)
            {
                _supplier.RegisterParameter(parameter);
            }
        }

        public void AddParameter(ParameterB parameter)
        {
            _parameters.Add(parameter);
            parameter.OnUpdate += sender => OnSetUpdate?.Invoke(sender);
            if(_supplier != null)
            {
                _supplier.RegisterParameter(parameter);
            }
        }

        public event ParamterUpdateHandler OnSetUpdate = delegate { };
    }
}
