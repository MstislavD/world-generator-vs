using System.ComponentModel;
using System.Windows.Forms;
using Utilities;
using WorldSimulation;
using WorldSimulation.HistorySimulation;

namespace WorldSimulationForm
{
    /// <summary>Simulator form for the legacy world generator, with history simulation, log and paedia windows.</summary>
    [DesignerCategory("")]
    public class LegacyWorldSimulatorForm : WorldSimulatorBaseForm
    {
        bool _trackedEvents = true;
        bool _printLog = false;

        Button _btnNextEvent = null!; // assigned in Initialize()
        LogForm _logForm = null!;     // assigned in Initialize()
        PaediaForm _paediaForm = null!; // assigned in Initialize()

        protected override void Initialize(ParametersPanel panel)
        {
            WorldGeneratorLegacy generator = new();

            generator.LogUpdated += Generator_LogUpdated;

            _generator = new LegacyGeneratorAdapter(generator);
            _generator.OnGenerationComplete += _renderMap;

            _gridLevel = new ParameterArray("Grid level", _generator.GridLevels, Enumerable.Range(0, _generator.GridLevels + 1).Cast<object>());

            _mapMode.Update(this, MapMode.Biomes);

            _mapSettings.Add(_gridLevel);
            _mapSettings.Add(_mapMode);
            //_mapSettings.Add(_regionBorder);
            _mapSettings.Add(_subregionBorder);
            _mapSettings.Add(_texture);
            _mapSettings.RegisterProvider(panel);

            _generationSettings.Add(_regenerate);
            _generationSettings.RegisterProvider(panel);

            _generator.Parameters.RegisterProvider(panel);

            Button btnLog = panel.AddButton("Log");
            btnLog.Click += BtnLog_Click;

            Button btnPaedia = panel.AddButton("Paedia");
            btnPaedia.Click += BtnPaedia_Click;

            _btnNextEvent = panel.AddButton("Next Event");
            _btnNextEvent.Enabled = false;
            _btnNextEvent.Click += BtnNextEvent_Click;

            _logForm = new LogForm();
            _paediaForm = new PaediaForm();
            _paediaForm.RaceHoverBegin += RaceHoverBegin;
            _paediaForm.RegionHoverBegin += RegionHoverBegin;
            _paediaForm.RaceHoverEnd += RaceHoverEnd;
            _paediaForm.RegionHoverEnd += RegionHoverEnd;
        }

        protected override void SwitchToSibling()
        {
            Form? existing = Application.OpenForms.OfType<NewWorldSimulatorForm>().FirstOrDefault();
            if (existing != null)
            {
                existing.Visible = true;
                existing.Activate();
            }
            else
                new NewWorldSimulatorForm(); // constructor shows it maximized

            Visible = false; // stay alive (hidden) so the generated world is kept for the next switch
        }

        protected override SubregionGraph? MapGraph => _generator.SubregionGraph;

        protected override (double Width, double Height) WorldSize()
        {
            SubregionGraph? graph = _generator.SubregionGraph;
            return graph != null ? (graph.Width, graph.Height) : (0, 0);
        }

        protected override void OnGenerationStarted()
        {
            _logForm.Clear();
            _paediaForm.InitializeHistory(_generator);
            _generator.History.EventLogged += History_EventLogged;
        }

        protected override void OnWorldRendered()
        {
            if (_generator.History.EventCount == 0)
            {
                _btnNextEvent.Text = "Next Event";
                _btnNextEvent.Enabled = !_generator.History.IsFinished;
                _currentEvent = null;
            }
            else
            {
                _btnNextEvent.Text = $"Next ({_generator.History.Turn})";
            }

            if (_logForm.Visible)
            {
                _logForm.Update();
                _logForm.Focus();
            }
        }

        protected override void OnSubregionClicked(WorldSimulation.Region region)
        {
            _paediaForm.OnRegionSelected(region);
            if (!_paediaForm.Visible)
                _paediaForm.Show();
            _paediaForm.Focus();
        }

        private void BtnNextEvent_Click(object? sender, EventArgs e)
        {
            var hist = _generator.History;

            if (sender == null || hist == null) return;

            int eventsCount = ModifierKeys switch
            {
                Keys.Alt => 1000,
                Keys.Shift => 100,
                Keys.Control => 10,
                _ => 1
            };
            _currentEvent = _trackedEvents ? hist.NextTrackedEvent() : hist.NextEvents(eventsCount);

            _paediaForm.RefreshRaces(); // events may have created new races — refresh the open window
            _renderMap(sender, e);
        }

        private void BtnPaedia_Click(object? sender, EventArgs e)
        {
            if (!_paediaForm.Visible)
                _paediaForm.Show();
            else
                _paediaForm.Hide();
        }

        private void BtnLog_Click(object? sender, EventArgs e)
        {
            if (!_logForm.Visible)
                _logForm.Show();
            else
                _logForm.Hide();
        }

        private void Generator_LogUpdated(object? sender, string entry)
        {
            if (_printLog && sender is HistorySimulator)
            {
                _logForm.AddEntry(entry);
            }               
        }

        private void History_EventLogged(object? sender, HistoricEvent e)
        {
            string info = $"T{_generator.History.Turn}: {e.Info}";
            _logForm.AddEntry(info);
        }
    }
}
