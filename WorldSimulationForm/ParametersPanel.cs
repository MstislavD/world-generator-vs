using System.ComponentModel;
using System.Diagnostics;
using Utilities;

namespace WorldSimulationForm
{
    [DesignerCategory("")]
    public class ParametersPanel : FlowLayoutPanel, IParameterProvider
    {
        ToolTip _tooltip = new ToolTip();
        Random _random = new Random();

        public event EventHandler<Parameter> OnParameterUpdate = delegate { };

        public void RegisterParameter(Parameter parameter)
        {
            bool added = parameter switch
            {
                ParameterArray p => AddComboBox(p),
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
            chb.CheckedChanged += (sender, e) => { parameter.Update(chb, chb.Checked); OnParameterUpdate.Invoke(chb, parameter); };
            parameter.OnUpdate += (sender, e) => chb.Checked = chb.Equals(sender) ? chb.Checked : parameter.Current;
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
            text.Click += (sender, e) => { int r = _random.Next(); text.Text = r.ToString(); seed.Update(text, r); OnParameterUpdate.Invoke(text, seed); };
            seed.OnUpdate += (sender, e) => text.Text = text.Equals(sender) ? text.Text : seed.ToString();
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
            numeric.ValueChanged += (sender, e) => { parameter.Update(numeric, (int)numeric.Value); OnParameterUpdate.Invoke(numeric, parameter); };
            parameter.OnUpdate += (sender, e) => numeric.Value = numeric.Equals(sender) ? numeric.Value : parameter.Current;
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
            numeric.ValueChanged += (sender, e) => { parameter.Update(numeric, (double)numeric.Value); OnParameterUpdate.Invoke(numeric, parameter); };
            parameter.OnUpdate += (sender, e) => numeric.Value = numeric.Equals(sender) ? numeric.Value : (decimal)parameter.Current;
            _tooltip.SetToolTip(numeric, parameter.Name);
            Controls.Add(numeric);

            AddLabel(parameter.Name);

            return true;
        }

        private bool AddComboBox(ParameterArray parameter)
        {
            ComboBox combo = new ComboBox();
            combo.Items.AddRange(parameter.PossibleValues.Cast<object>().ToArray());
            combo.SelectedItem = parameter.Current;
            combo.SelectedIndexChanged += (sender, e) => { parameter.Update(combo, combo.SelectedItem); OnParameterUpdate.Invoke(combo, parameter); };
            parameter.OnUpdate += (sender, e) => combo.SelectedItem = combo.Equals(sender) ? combo.SelectedItem : parameter;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Width = Width - combo.Margin.Left * 2;
            _tooltip.SetToolTip(combo, parameter.Name);
            Controls.Add(combo);
            return true;
        }
    }
}
