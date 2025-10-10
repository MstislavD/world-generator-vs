using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

//using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Parameters;
using static System.Net.Mime.MediaTypeNames;
using Parameter = Parameters.Parameter;

namespace WorldSimulationForm
{
    [DesignerCategory("")]
    public class ParametersPanel : FlowLayoutPanel, IParameterSupplier, IParameterBSupplier
    {
        Dictionary<Parameter, CheckBox> _chbByParameter = new Dictionary<Parameter, CheckBox>();
        Dictionary<Parameter, TextBox> _textByParameter = new Dictionary<Parameter, TextBox>();
        Dictionary<Parameter, NumericUpDown> _numericByParameter = new Dictionary<Parameter, NumericUpDown>();
        Dictionary<Parameter, ComboBox> _cmbByParameter = new Dictionary<Parameter, ComboBox>();

        Dictionary<ParameterB, Control> _controlByParameter = new();
        Dictionary<Control, ParameterB> _parameterByControl = new();

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
                    text.Click += (s, e) => _newRandomSeed(text, parameter);
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

        private void _newRandomSeed(TextBox text, Parameter parameter)
        {
            int newSeed = _random.Next();
            text.Text = newSeed.ToString();
            OnParameterUpdate?.Invoke(this, parameter);
        }

        public void RegisterParameter(ParameterB parameter)
        {
            Type t = parameter.GetType();
            Debug.WriteLine(t);

            bool added = parameter switch
            {
                IParameterArray p => AddComboBox(p),
                Parameter<bool> p => AddCheckBox(p),
                ParameterSeed s => AddTextBox(s),
                ParameterRange<int> p => AddNumericUpDown(p),
                ParameterRange<double> p => AddNumericUpDown(p),
                _ => false
            };
        }
        public Label AddLabel(string text)
        {
            Label lbl = new Label() { Text = text };
            lbl.Size = new Size(Width - lbl.Margin.All * 2, Width / 3);
            Controls.Add(lbl);
            return lbl;
        }
        public Button AddButton(string text)
        {
            Button btn = new Button() { Text = text };
            btn.Size = new Size(Width - btn.Margin.All * 2, Width / 3);
            Controls.Add(btn);
            return btn;
        }

        private bool AddCheckBox(Parameter<bool> parameter)
        {
            CheckBox chb = new CheckBox();
            chb.Checked = parameter.Current;
            chb.CheckedChanged += (sender, e) => parameter.Update(chb, chb.Checked);
            parameter.OnUpdate += (sender) => chb.Checked = sender.Equals(chb) ? chb.Checked : parameter.Current;
            chb.Text = parameter.Name;
            chb.Width = Width - chb.Margin.Left - chb.Margin.Right;
            chb.Height = chb.Width / 3;
            _tooltip.SetToolTip(chb, chb.Text);
            Controls.Add(chb);
            return true;
        }

        private bool AddTextBox(Parameter<int> seed)
        {
            TextBox text = new TextBox();
            text.Text = seed.Current.ToString();
            text.Click += (sender, e) => { int r = _random.Next(); text.Text = r.ToString(); seed.Update(text, r); };
            seed.OnUpdate += (sender) => text.Text = sender.Equals(text) ? text.Text : seed.ToString();
            text.ReadOnly = true;
            text.Width = Width - text.Margin.Left - text.Margin.Right;
            _tooltip.SetToolTip(text, seed.Name);
            Controls.Add(text);
            return true;
        }

        private bool AddNumericUpDown(ParameterRange<int> parameter)
        {
            NumericUpDown numeric = new NumericUpDown();
            numeric.AutoSize = true;
            numeric.Minimum = parameter.Min;
            numeric.Maximum = parameter.Max;
            numeric.Value = parameter.Current;
            numeric.Increment = 1m;
            numeric.ValueChanged += (sender, e) => parameter.Update(numeric, (int)numeric.Value);
            parameter.OnUpdate += (sender) => numeric.Value = sender.Equals(numeric) ? numeric.Value : parameter.Current;
            _tooltip.SetToolTip(numeric, parameter.Name);
            Controls.Add(numeric);

            AddLabel(parameter.Name);

            return true;
        }

        private bool AddNumericUpDown(ParameterRange<double> parameter)
        {
            NumericUpDown numeric = new NumericUpDown();
            numeric.AutoSize = true;
            numeric.Minimum = (decimal)parameter.Min;
            numeric.Maximum = (decimal)parameter.Max;
            numeric.Value = (decimal)parameter.Current;
            numeric.DecimalPlaces = 2;
            numeric.Increment = 0.01m;
            numeric.ValueChanged += (sender, e) => parameter.Update(numeric, (double)numeric.Value);
            parameter.OnUpdate += (sender) => numeric.Value = sender.Equals(numeric) ? numeric.Value : (decimal)parameter.Current;
            _tooltip.SetToolTip(numeric, parameter.Name);
            Controls.Add(numeric);

            AddLabel(parameter.Name);

            return true;
        }

        private bool AddComboBox(IParameterArray parameter)
        {
            ComboBox combo = new ComboBox();
            combo.Items.AddRange(parameter.PossibleValues.Cast<object>().ToArray());
            combo.SelectedItem = parameter.Current;
            combo.SelectedIndexChanged += (sender, e) => parameter.Parameter.Update(combo, combo.SelectedItem);
            parameter.Parameter.OnUpdate += (sender) => combo.SelectedItem = sender.Equals(combo) ? combo.SelectedItem : parameter;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Width = Width - combo.Margin.Left * 2;
            _tooltip.SetToolTip(combo, parameter.Parameter.Name);
            Controls.Add(combo);
            return true;
        }
    }
}
