using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WorldSimulation;
using WorldSimulation.HistorySimulation;

namespace WorldSimulationForm
{
    enum ViewMode {Init, Races, Race, Region }

    public partial class PaediaForm : Form
    {
        FlowLayoutPanel _itemPanel;
        WorldGenerator _generator;
        Stack<ViewMode> _viewModes;
        Stack<Race> _viewRaces;
        Stack<WorldSimulation.Region> _viewRegions;
        ViewMode _currentMode;
        Race _currentRace;
        WorldSimulation.Region _currentRegion;
        bool _back = false;

        public PaediaForm()
        {
            InitializeComponent();

            _viewModes = new Stack<ViewMode>();
            _viewRaces = new Stack<Race>();
            _viewRegions = new Stack<WorldSimulation.Region>();

            _currentMode = ViewMode.Init;

            FormClosing += LogForm_FormClosing;
            VisibleChanged += PaediaForm_Shown;
            MouseEnter += PaediaForm_MouseEnter;

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.MouseEnter += PaediaForm_MouseEnter;
            Controls.Add(panel);

            FlowLayoutPanel pnlButtons = new FlowLayoutPanel();
            pnlButtons.AutoSize = true;
            panel.Controls.Add(pnlButtons);

            Button btnBack = new Button();
            btnBack.Text = "<";
            btnBack.AutoSize = true;
            btnBack.Click += BtnBack_Click;
            pnlButtons.Controls.Add(btnBack);

            Button btnRaces = new Button();
            btnRaces.Text = "Races";
            btnRaces.AutoSize = true;
            btnRaces.Click += (s, e) => _showRaces();
            pnlButtons.Controls.Add(btnRaces);

            _itemPanel = new FlowLayoutPanel();
            _itemPanel.FlowDirection = FlowDirection.TopDown;
            _itemPanel.AutoScroll = true;
            _itemPanel.WrapContents = false;
            _itemPanel.MouseEnter += PaediaForm_MouseEnter;
            panel.Controls.Add(_itemPanel);

            PaediaForm_Resize(this, new EventArgs());
            Resize += PaediaForm_Resize;
        }

        private void PaediaForm_Resize(object sender, EventArgs e)
        {
            _itemPanel.Width = ClientRectangle.Width;
            _itemPanel.Height = ClientRectangle.Height - 40;
        }

        private void PaediaForm_MouseEnter(object sender, EventArgs e)
        {
            if (_currentMode == ViewMode.Race)
                RaceHoverBegin?.Invoke(this, _currentRace);
            else if (_currentMode == ViewMode.Region)
                RegionHoverBegin?.Invoke(this, _currentRegion);
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (_viewModes.Count > 0)
            {
                _back = true;
                ViewMode mode = _viewModes.Pop();
                if (mode == ViewMode.Races)
                {
                    _showRaces();
                }
                else if (mode == ViewMode.Race)
                {
                    Race race = _viewRaces.Pop();
                    OnRaceSelected(race);
                }
                else if (mode == ViewMode.Region)
                {
                    WorldSimulation.Region region = _viewRegions.Pop();
                    OnRegionSelected(region);
                }
            }
        }

        public void InitializeHistory(WorldGenerator generator)
        {
            _generator = generator;

            _viewModes = new Stack<ViewMode>();
            _viewRaces = new Stack<Race>();
            _viewRegions = new Stack<WorldSimulation.Region>();
            _currentMode = ViewMode.Init;

            _itemPanel.Controls.Clear();
        }

        public event EventHandler<Race> RaceHoverBegin;
        public event EventHandler RaceHoverEnd;
        public event EventHandler<WorldSimulation.Region> RegionHoverBegin;
        public event EventHandler<WorldSimulation.Region> RegionHoverEnd;

        private void PaediaForm_Shown(object sender, EventArgs e)
        {
            if (Visible && _currentMode == ViewMode.Init)
                _showRaces();
        }

        private void _showRaces()
        {
            _pushCurrentMode();
            _currentMode = ViewMode.Races;

            foreach (Race race in _generator.History.Races.OrderBy(r => r.Name))
            {
                string info = "";
                foreach (RacialTrait.Tag trait in race.Tags)
                    info += $"{trait}, ";
                info = info.Substring(0, info.Length - 2).ToLower();

                Label lblRace = _selectableLabel(race.Name + $" ({info})");
                lblRace.MouseEnter += (s, e) => RaceHoverBegin?.Invoke(this, race);
                lblRace.MouseLeave += (s, e) => RaceHoverEnd?.Invoke(race, e);
                lblRace.MouseClick += (s, e) => OnRaceSelected(race);
            }
        }

        private void _pushCurrentMode()
        {
            if (_currentMode == ViewMode.Init)
                return;

            _itemPanel.Controls.Clear();

            if (_back == true)
            {
                _back = false;
                return;
            }

            _viewModes.Push(_currentMode);
            if (_currentMode == ViewMode.Race)
            {
                _viewRaces.Push(_currentRace);
            }
            else if (_currentMode == ViewMode.Region)
            {
                _viewRegions.Push(_currentRegion);
            }            
        }

        public void OnRegionSelected(WorldSimulation.Region region)
        {
            _pushCurrentMode();
            _currentMode = ViewMode.Region;
            _currentRegion = region;

            _header(region.Name);
            _label(region.Center.ToString());
            _label($"Climate: {region.Belt} {region.Humidity}");
            _label($"Biome: {region.Biome}" + (region.IsRidge ? " (Ridge)" : ""));            

            foreach (Population pop in region.Pops)
            {
                Label lblPop = _selectableLabel(pop.Race.Name);
            }

            RegionHoverBegin(this, region);
        }

        private void OnRaceSelected(Race race)
        {
            _pushCurrentMode();
            _currentMode = ViewMode.Race;
            _currentRace = race;

            Label lblName = _header(race.Name);

            foreach(var tag in race.Tags)
                _label(tag.ToString());

            foreach (var trait in race.Traits)
                _label(trait.Name);

            int popCount = _generator.History.CountPops(race);
            Label lblPopCount = _label($"Pops: {popCount}");
        }

        private void LogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        Label _header(string text)
        {
            Label lbl = _label(text);
            lbl.Text = text.ToUpper();
            lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            lbl.ForeColor = Color.Blue;
            return lbl;
        }

        Label _label(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Width = 1000;
            lbl.MouseEnter += PaediaForm_MouseEnter;
            _itemPanel.Controls.Add(lbl);
            return lbl;
        }

        Label _selectableLabel(string text)
        {
            Label lbl = _label(text);
            lbl.MouseEnter += (s, e) => lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            lbl.MouseLeave += (s, e) => lbl.Font = new Font(lbl.Font, FontStyle.Regular);
            return lbl;
        }
    }
}
