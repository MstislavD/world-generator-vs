using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorldSimulator
{
    public partial class LogForm : Form
    {
        FlowLayoutPanel _logPanel;
        Color _labelColor;

        public LogForm()
        {
            FormClosing += LogForm_FormClosing;

            Width = 800;
            Height = 800;

            _labelColor = Color.White;

            _logPanel = new FlowLayoutPanel();
            _logPanel.AutoSize = true;
            _logPanel.FlowDirection = FlowDirection.TopDown;
            _logPanel.WrapContents = false;
            _logPanel.Width = Width;
            _logPanel.MaximumSize = new Size(ClientRectangle.Width, ClientRectangle.Height);
            _logPanel.AutoScroll = true;

            SizeChanged += LogForm_SizeChanged;

            Controls.Add(_logPanel);
        }

        private void LogForm_SizeChanged(object sender, EventArgs e)
        {
            _logPanel.MaximumSize = new Size(ClientRectangle.Width, ClientRectangle.Height);
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        public void AddEntry(string entry)
        {
            Label lblEntry = new Label();
            //lblEntry.AutoSize = true;
            lblEntry.Width = _logPanel.Width - 30;
            lblEntry.BackColor = _labelColor;
            lblEntry.Text = entry;

            _logPanel.Controls.Add(lblEntry);
            _labelColor = _labelColor == Color.White ? Color.Lavender : Color.White;
        }

        public void Clear()
        {
            _logPanel.Controls.Clear();
            _labelColor = Color.White;
        }
        public void Update()
        {
            _logPanel.VerticalScroll.Value = _logPanel.VerticalScroll.Maximum;
        }


    }
}
