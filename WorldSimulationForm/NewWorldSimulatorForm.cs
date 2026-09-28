using System.ComponentModel;
using System.Windows.Forms;
using Utilities;
using WorldSimulation;

namespace WorldSimulationForm
{
    /// <summary>Simulator form for the new world generator.</summary>
    [DesignerCategory("")]
    public class NewWorldSimulatorForm : WorldSimulatorBaseForm
    {
        WorldGenerator _worldGenerator = null!; // assigned in Initialize()

        Parameter<bool> _showTileCounts = new("Tile counts", false);
        Label _lblTileCounts;

        public NewWorldSimulatorForm()
        {
            Text = "World Generator";

            // The toggle is registered after the base form built the panel, so it can be placed
            // right above the Info label at the bottom of the panel. It goes into
            // _generationSettings, so toggling it neither re-renders the map nor regenerates
            // the world; the info line is refreshed by OnWorldRendered instead.
            _generationSettings.Add(_showTileCounts);
            Control checkbox = _panel.Controls[_panel.Controls.Count - 1];
            Label infoLabel = _panel.Controls.OfType<Label>().Last();
            _panel.Controls.SetChildIndex(checkbox, _panel.Controls.IndexOf(infoLabel));

            // The line of tile counts for the selected layer, shown in the lower part of the window.
            // AutoSize (not a fixed pixel height) so the strip fits the font at any DPI:
            // with Dock=Bottom the label sits flush at the bottom of the client area and
            // sizes itself to the text (a fixed 20px height clipped the text in half at
            // 150%+ scaling, where the 9pt line needs ~41px).
            _lblTileCounts = new Label() { Dock = DockStyle.Bottom, AutoSize = true, Visible = false };
            Controls.Add(_lblTileCounts);

            _showTileCounts.OnUpdate += (s, e) => _updateTileCountsInfo();
        }

        protected override void Initialize(ParametersPanel panel)
        {
            _worldGenerator = new WorldGenerator();
            _generator = new GeneratorAdapter(_worldGenerator);
            _generator.OnGenerationComplete += _renderMap;
            _gridLevel = new ParameterArray("Grid level", _generator.GridLevels - 1, Enumerable.Range(0, _generator.GridLevels).Cast<object>());

            _mapSettings.Add(_gridLevel);
            _mapSettings.RegisterProvider(panel);

            _generationSettings.Add(_regenerate);
            _generationSettings.RegisterProvider(panel);

            _generator.Parameters.RegisterProvider(panel);
        }

        protected override void OnWorldRendered() => _updateTileCountsInfo();

        private void _updateTileCountsInfo()
        {
            WorldGrid? grid = _worldGenerator.Grid((int)_gridLevel.Current);
            bool hasData = _showTileCounts && grid != null && _worldGenerator.GenerationIsComplete;
            _lblTileCounts.Visible = hasData;
            if (!hasData) return;

            int land = 0, sea = 0;
            foreach (WorldCell cell in grid.Cells)
            {
                if (_worldGenerator.IsLand(cell))
                    land++;
                else
                    sea++;
            }
            _lblTileCounts.Text = $"Land: {land} | Sea: {sea}";
        }

        protected override void SwitchToSibling()
        {
            Form? existing = Application.OpenForms.OfType<LegacyWorldSimulatorForm>().FirstOrDefault();
            if (existing != null)
            {
                existing.Visible = true;
                existing.Activate();
            }
            else
                new LegacyWorldSimulatorForm(); // constructor shows it maximized

            Visible = false; // stay alive (hidden) so the generated world is kept for the next switch
        }

        protected override (double Width, double Height) WorldSize()
        {
            WorldGrid? grid = _generator.GetGrid((int)_gridLevel.Current);
            return grid != null ? (grid.Width, grid.Height) : (0, 0);
        }
    }
}
