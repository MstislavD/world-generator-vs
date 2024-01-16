using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public interface IParameterSupplier
    {
        ParameterValue GetValue(Parameter parameter);
        event EventHandler<Parameter> OnParameterUpdate;
    }
}
