using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    public interface IParameterSet
    {
        IEnumerable<Parameter> Parameters { get; }
        event EventHandler OnSetUpdate;
    }

    public interface IParameterBSet
    {
        IEnumerable<ParameterB> Parameters { get; }
        event EventHandler OnSetUpdate;
    }
}
