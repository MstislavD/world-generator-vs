using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulationForm
{
    public partial class WorldSimulatorForm
    {
        bool _trackedEvents = true;

        private void BtnNextEvent_Click(object sender, EventArgs e)
        {
            if (sender == null || _generator.History == null) return;

            if (_trackedEvents)
            {
                _currentEvent = _generator.History.NextTrackedEvent();
            }
            else
            {
                if (ModifierKeys == Keys.Alt)
                    _currentEvent = _generator.History.NextEvents(1000);
                else if (ModifierKeys == Keys.Shift)
                    _currentEvent = _generator.History.NextEvents(100);
                else if (ModifierKeys == Keys.Control)
                    _currentEvent = _generator.History.NextEvents(10);
                else
                    _currentEvent = _generator.History.NextEvent();
            }          

            _updateAfterEvents(sender);
        }

        private void _updateAfterEvents(object sender)
        {
            (sender as Button).Text = _generator.GenerationIsComplete ? $"Next ({_generator.History.Turn})" : "Next Event";

            if (_generator.History.IsComplete)
            {
                (sender as Button).Enabled = false;
            }
            else
            {
                _redrawMap(this, null);
                if (_logForm.Visible)
                {
                    _logForm.Update();
                    _logForm.Focus();
                }
            }
        }
    }
}
