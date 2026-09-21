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

    [DesignerCategory("")]
    public partial class PaediaForm : Form
    {
        FlowLayoutPanel _itemPanel;
        Button _btnBack;
        IGenerator _generator;
        Stack<ViewMode> _viewModes;
        Stack<Race> _viewRaces;
        Stack<WorldSimulation.Region> _viewRegions;
        ViewMode _currentMode;
        Race? _currentRace;
        WorldSimulation.Region? _currentRegion;
        Race? _hoveredRace; // race whose list label is hovered (Races view) — its highlight must be dropped when the list is cleared
        bool _back = false;
        readonly Font _boldFont;

        public PaediaForm()
        {
            Text = "Paedia";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 620);
            MinimumSize = new Size(320, 240);

            // single shared bold font: assigning a fresh Font on every hover leaked GDI handles.
            // Derive it from a label's actual (DPI-scaled) default font, NOT SystemFonts.DefaultFont:
            // on high-DPI displays the latter resolves to a smaller font, so hovering shrank the text
            // lines and shifted the rows below — labels kept jumping under the cursor and the
            // Enter/Leave events flickered. A sample Label allocates no GDI resources (no handle).
            _boldFont = new Font(new Label().Font, FontStyle.Bold);

            _viewModes = new Stack<ViewMode>();
            _viewRaces = new Stack<Race>();
            _viewRegions = new Stack<WorldSimulation.Region>();

            _currentMode = ViewMode.Init;

            FormClosing += PaediaForm_FormClosing;
            VisibleChanged += PaediaForm_VisibleChanged;
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
            btnBack.Enabled = false;
            btnBack.Click += BtnBack_Click;
            _btnBack = btnBack;
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
            _itemPanel.Height = Math.Max(0, ClientRectangle.Height - 40);
            _applyLabelWidths();
        }

        // labels span the item panel (minus the vertical scrollbar), not a fixed 1000px
        private int _labelWidth() => Math.Max(100, ClientRectangle.Width - SystemInformation.VerticalScrollBarWidth);

        private void _applyLabelWidths()
        {
            int width = _labelWidth();
            foreach (Control c in _itemPanel.Controls)
                if (c is Label lbl)
                {
                    lbl.Width = width;
                    _fitHeight(lbl); // wrapping changes when the width changes
                }
        }

        // Two height rules, both needed for stable hover:
        // (1) a label's default height (23px) is a 96-DPI value that does not scale with the display —
        //     on high-DPI screens each text line needs ~47px, so stacked entries overlapped; fit the
        //     height to the wrapped text instead.
        // (2) measure with the BOLD font: bold glyphs are wider, so regular text wraps to no more lines
        //     than the same text in bold. The label's height is then identical before and after the hover
        //     font swap — a height change on hover shifts the rows below and re-triggers Enter/Leave
        //     under the cursor (flicker).
        private void _fitHeight(Label lbl)
        {
            Label probe = new Label { Text = lbl.Text, Font = _boldFont, Width = lbl.Width };
            int h = probe.GetPreferredSize(new Size(lbl.Width, int.MaxValue)).Height;
            if (h > 0)
                lbl.Height = h;
        }

        private void PaediaForm_MouseEnter(object sender, EventArgs e)
        {
            if (_currentMode == ViewMode.Race)
                RaceHoverBegin?.Invoke(this, _currentRace!); // mode guarantees a current race
            else if (_currentMode == ViewMode.Region)
                RegionHoverBegin?.Invoke(this, _currentRegion!); // mode guarantees a current region
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

        // re-renders the race list if it is currently shown — called by the main form after an event
        // batch, so races created by new events appear in the open window without clicking "Races"
        // or reopening it (right after Start the world legitimately has no races yet)
        public void RefreshRaces()
        {
            if (Visible && _currentMode == ViewMode.Races)
                _showRaces();
        }

        public void InitializeHistory(IGenerator generator)
        {
            _generator = generator;

            // the old world is gone — drop any map highlight its view was responsible for
            if (_currentMode == ViewMode.Race && _currentRace != null)
                RaceHoverEnd?.Invoke(this, _currentRace);
            else if (_currentMode == ViewMode.Region && _currentRegion != null)
                RegionHoverEnd?.Invoke(this, _currentRegion);

            _viewModes = new Stack<ViewMode>();
            _viewRaces = new Stack<Race>();
            _viewRegions = new Stack<WorldSimulation.Region>();
            _currentMode = ViewMode.Init;
            _currentRace = null;
            _currentRegion = null;
            _back = false;

            _clearItems(); // also drops the hovered-race highlight of the old list, if any
            _updateBackButton();

            if (Visible)
                _showRaces();
        }

        public event EventHandler<Race> RaceHoverBegin;
        public event EventHandler<Race> RaceHoverEnd;
        public event EventHandler<WorldSimulation.Region> RegionHoverBegin;
        public event EventHandler<WorldSimulation.Region> RegionHoverEnd;

        private void PaediaForm_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                // re-showing may reveal a world that changed while hidden (regeneration, events) —
                // refresh the race list; detail views keep their place
                if (_currentMode == ViewMode.Init || _currentMode == ViewMode.Races)
                    _showRaces();
            }
            else
            {
                // window hidden — drop any map highlight this view was responsible for
                if (_currentMode == ViewMode.Race && _currentRace != null)
                    RaceHoverEnd?.Invoke(this, _currentRace);
                else if (_currentMode == ViewMode.Region && _currentRegion != null)
                    RegionHoverEnd?.Invoke(this, _currentRegion);
            }
        }

        // clears the item panel. If a race label was hovered when it is cleared, its MouseLeave
        // never fires (the control is destroyed), so drop the map highlight it set manually.
        private void _clearItems()
        {
            if (_hoveredRace != null)
            {
                RaceHoverEnd?.Invoke(this, _hoveredRace);
                _hoveredRace = null;
            }
            _itemPanel.Controls.Clear();
        }

        private void _showRaces()
        {
            // always re-render from current data — the world may have changed since the last render
            // (regeneration, new events). Only skip pushing a history entry when already here.
            if (_currentMode != ViewMode.Races)
                _pushCurrentMode();
            _clearItems();
            _currentMode = ViewMode.Races;

            if (_generator?.History == null)
            {
                _label("No world generated yet — press Start first.");
                _updateBackButton();
                return;
            }

            if (!_generator.History.Races.Any())
            {
                _label("No races in this world yet — run some events.");
                _updateBackButton();
                return;
            }

            foreach (Race race in _generator.History.Races.OrderBy(r => r.Name))
            {
                string info = "";
                foreach (RacialTrait.Tag tag in race.Tags.OrderBy(t => t))
                    info += $"{tag.ToString().ToLowerInvariant()}, ";
                if (info.Length > 0)
                    info = info.Substring(0, info.Length - 2); // zero-tag races: nothing to trim

                string text = info.Length > 0 ? $"{race.Name} ({info})" : race.Name;
                Label lblRace = _selectableLabel(text);
                lblRace.MouseEnter += (s, e) => { _hoveredRace = race; RaceHoverBegin?.Invoke(this, race); };
                lblRace.MouseLeave += (s, e) => { if (_hoveredRace == race) _hoveredRace = null; RaceHoverEnd?.Invoke(this, race); };
                lblRace.MouseClick += (s, e) => OnRaceSelected(race);
            }
            _updateBackButton();
        }

        private void _pushCurrentMode()
        {
            if (_currentMode == ViewMode.Init)
            {
                _updateBackButton();
                return;
            }

            // leaving a detail view — drop its map highlight before switching content
            if (_currentMode == ViewMode.Race && _currentRace != null)
                RaceHoverEnd?.Invoke(this, _currentRace);
            else if (_currentMode == ViewMode.Region && _currentRegion != null)
                RegionHoverEnd?.Invoke(this, _currentRegion);

            _clearItems();

            if (_back == true)
            {
                _back = false;
                _updateBackButton();
                return;
            }

            _viewModes.Push(_currentMode);
            if (_currentMode == ViewMode.Race)
            {
                _viewRaces.Push(_currentRace!); // mode guarantees a current race
            }
            else if (_currentMode == ViewMode.Region)
            {
                _viewRegions.Push(_currentRegion!); // mode guarantees a current region
            }

            _updateBackButton();
        }

        private void _updateBackButton()
        {
            if (_btnBack != null)
                _btnBack.Enabled = _viewModes.Count > 0;
        }

        public void OnRegionSelected(WorldSimulation.Region region)
        {
            _pushCurrentMode();
            _currentMode = ViewMode.Region;
            _currentRegion = region;

            _header(region.Name);
            _label($"Center: {region.Center}");
            _label($"Climate: {region.Belt} {region.Humidity}");
            _label($"Biome: {region.Biome}" + (region.IsRidge ? " (Ridge)" : ""));

            foreach (Population pop in region.Pops)
            {
                Label lblPop = _selectableLabel(pop.Race.Name);
                lblPop.MouseClick += (s, e) => OnRaceSelected(pop.Race);
            }

            RegionHoverBegin?.Invoke(this, region);
            _updateBackButton();
        }

        private void OnRaceSelected(Race race)
        {
            _pushCurrentMode();
            _currentMode = ViewMode.Race;
            _currentRace = race;

            _header(race.Name);

            foreach (var tag in race.Tags.OrderBy(t => t))
                _label(tag.ToString().ToLowerInvariant());

            foreach (var trait in race.Traits.OrderBy(t => t.Name))
                _label(trait.Name);

            int popCount = _generator.History.CountPops(race);
            _label($"Pops: {popCount}");
            _updateBackButton();
        }

        private void PaediaForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        Label _header(string text)
        {
            Label lbl = _label(text);
            lbl.Text = text.ToUpperInvariant();
            lbl.Font = _boldFont;
            _fitHeight(lbl); // uppercasing + bold can change the wrap count
            lbl.ForeColor = Color.Blue;
            return lbl;
        }

        Label _label(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Width = _labelWidth();
            _fitHeight(lbl);
            lbl.MouseEnter += PaediaForm_MouseEnter;
            _itemPanel.Controls.Add(lbl);
            return lbl;
        }

        Label _selectableLabel(string text)
        {
            Label lbl = _label(text);
            Font regular = lbl.Font; // shared default font, not owned by the label
            // font swap only — the height is already bold-fitted (see _fitHeight), so hovering
            // changes no geometry and cannot re-trigger Enter/Leave under the cursor
            lbl.MouseEnter += (s, e) => lbl.Font = _boldFont;
            lbl.MouseLeave += (s, e) => lbl.Font = regular;
            return lbl;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _boldFont?.Dispose();

            base.Dispose(disposing);
        }
    }
}
