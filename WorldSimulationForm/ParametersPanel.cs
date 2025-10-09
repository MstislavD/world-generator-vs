using Parameters;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorldSimulationForm
{
    [DesignerCategory("")]
    public class ParametersPanel : FlowLayoutPanel, IParameterSupplier //, IParameterBSupplier
    {
        Dictionary<Parameter, CheckBox> _chbByParameter = new Dictionary<Parameter, CheckBox>();
        Dictionary<Parameter, TextBox> _textByParameter = new Dictionary<Parameter, TextBox>();
        Dictionary<Parameter, NumericUpDown> _numericByParameter = new Dictionary<Parameter, NumericUpDown>();
        Dictionary<Parameter, ComboBox> _cmbByParameter = new Dictionary<Parameter, ComboBox>();

        ToolTip _tooltip = new ToolTip();
        Random _random = new Random();

        public event EventHandler<Parameter> OnParameterUpdate = delegate { };
        public event EventHandler<ParameterB> OnParameterBUpdate = delegate { };

        public ParameterValue GetValue(Parameter parameter)
        {
            if (parameter.Type == ParameterType.Bool)
            {
                return new ParameterValue(_chbByParameter[parameter].Checked);
            }
            else if (parameter.IsSeed)
            {
                return new ParameterValue(int.Parse(_textByParameter[parameter].Text));
            }
            else if (parameter.Type == ParameterType.Int)
            {
                return new ParameterValue((int)_numericByParameter[parameter].Value);
            }
            else if (parameter.Type == ParameterType.Double)
            {
                return new ParameterValue((double)_numericByParameter[parameter].Value);
            }
            else if (parameter.Type == ParameterType.String)
            {
                return new ParameterValue((string)_cmbByParameter[parameter].SelectedItem);
            }
            else
            {
                return ParameterValue.Default;
            }
        }

        public void AddParameterControls(IParameterSet parameters)
        {
            foreach (Parameter parameter in parameters.Parameters)
            {
                if (parameter.Type == ParameterType.Bool)
                {
                    CheckBox chb = new CheckBox();
                    chb.Width = Width - chb.Margin.Left - chb.Margin.Right;
                    chb.Text = parameter.Name;
                    _chbByParameter[parameter] = chb;
                    chb.Checked = parameter;
                    chb.CheckedChanged += (s, e) => OnParameterUpdate?.Invoke(this, parameter);
                    _tooltip.SetToolTip(chb, chb.Text);
                    Controls.Add(chb);
                }
                else if (parameter.IsSeed)
                {
                    TextBox text = new TextBox();
                    text.Width = Width - text.Margin.Left - text.Margin.Right;
                    text.Text = parameter.Current.IntValue.ToString();
                    _textByParameter[parameter] = text;
                    text.Click += (s, e) => _newRandomSeed(text, e, parameter);
                    text.ReadOnly = true;
                    _tooltip.SetToolTip(text, parameter.Name);
                    Controls.Add(text);
                }
                else if (parameter.Type == ParameterType.Int)
                {
                    NumericUpDown numeric = new NumericUpDown();
                    numeric.AutoSize = true;
                    numeric.Minimum = parameter.Min.IntValue;
                    numeric.Maximum = parameter.Max.IntValue;
                    numeric.Value = parameter.Current.IntValue;
                    numeric.Increment = 1m;
                    _numericByParameter[parameter] = numeric;
                    numeric.ValueChanged += (s, e) => OnParameterUpdate?.Invoke(this, parameter);
                    _tooltip.SetToolTip(numeric, parameter.Name);

                    Label lbl = new Label();
                    lbl.Text = parameter.Name;
                    lbl.Width = Width - lbl.Margin.Left - lbl.Margin.Right;

                    Controls.Add(numeric);
                    Controls.Add(lbl);
                }
                else if (parameter.Type == ParameterType.Double)
                {
                    NumericUpDown numeric = new NumericUpDown();
                    numeric.AutoSize = true;
                    numeric.Minimum = (decimal)parameter.Min.DoubleValue;
                    numeric.Maximum = (decimal)parameter.Max.DoubleValue;
                    numeric.Value = (decimal)parameter.Current.DoubleValue;
                    numeric.DecimalPlaces = 2;
                    numeric.Increment = 0.01m;
                    _numericByParameter[parameter] = numeric;
                    numeric.ValueChanged += (s, e) => OnParameterUpdate?.Invoke(this, parameter);
                    _tooltip.SetToolTip(numeric, parameter.Name);

                    Label lbl = new Label();
                    lbl.Text = parameter.Name;
                    lbl.Width = Width - lbl.Margin.Left - lbl.Margin.Right;

                    Controls.Add(numeric);
                    Controls.Add(lbl);
                }
                else if (parameter.Type == ParameterType.String)
                {
                    ComboBox combo = new ComboBox();
                    foreach (string s in parameter.PossibleValues)
                    {
                        combo.Items.Add(s);
                    }
                    combo.SelectedItem = parameter.Current.StringValue;
                    combo.SelectedIndexChanged += (s, e) => OnParameterUpdate?.Invoke(this, parameter);
                    combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    combo.Width = Width - combo.Margin.Left * 2;
                    _cmbByParameter[parameter] = combo;
                    _tooltip.SetToolTip(combo, parameter.Name);

                    Controls.Add(combo);
                }
            }
        }

        private void _newRandomSeed(TextBox text, EventArgs e, Parameter parameter)
        {
            int newSeed = _random.Next();
            text.Text = newSeed.ToString();
            OnParameterUpdate?.Invoke(this, parameter);
        }

        //public object GetValue(ParameterB parameter)
        //{
        //    object value = parameter.GetType() switch
        //    {
        //        Parameter<bool> => _chbByParameter[parameter].Checked
        //    };
        //    if (parameter.Type == ParameterType.Bool)
        //    {
        //        return new ParameterValue(_chbByParameter[parameter].Checked);
        //    }
        //    else if (parameter.IsSeed)
        //    {
        //        return new ParameterValue(int.Parse(_textByParameter[parameter].Text));
        //    }
        //    else if (parameter.Type == ParameterType.Int)
        //    {
        //        return new ParameterValue((int)_numericByParameter[parameter].Value);
        //    }
        //    else if (parameter.Type == ParameterType.Double)
        //    {
        //        return new ParameterValue((double)_numericByParameter[parameter].Value);
        //    }
        //    else if (parameter.Type == ParameterType.String)
        //    {
        //        return new ParameterValue((string)_cmbByParameter[parameter].SelectedItem);
        //    }
        //    else
        //    {
        //        return ParameterValue.Default;
        //    }
        //}
    }
}
