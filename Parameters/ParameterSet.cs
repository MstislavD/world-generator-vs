using System;
using System.Collections.Generic;
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
}
