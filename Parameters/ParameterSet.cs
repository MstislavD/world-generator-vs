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

    public abstract class ParameterBSet : IParameterBSet
    {
        List<ParameterB> _parameters = new List<ParameterB>();

        public event EventHandler OnSetUpdate;

        public IEnumerable<ParameterB> Parameters => _parameters;

        protected ParameterBSet(IParameterBSupplier supplier)
        {
            supplier.OnParameterBUpdate += _updateParameter;
        }

        protected ParameterBSet() { }

        private void _updateParameter(object sender, ParameterB parameter)
        {
            IParameterBSupplier supplier = (IParameterBSupplier)sender;
            parameter.SetValue(supplier.GetValue(parameter));
            OnSetUpdate?.Invoke(this, new EventArgs());
        }

        protected void AddParameter(ParameterB parameter)
        {
            _parameters.Add(parameter);
        }

        protected void UpdateParameter(Parameter parameter, ParameterValue value)
        {
            parameter.SetValue(value);
            OnSetUpdate?.Invoke(this, new EventArgs());
        }
    }
}
