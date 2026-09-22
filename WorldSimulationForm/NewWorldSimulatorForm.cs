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
        public NewWorldSimulatorForm()
        {
            Text = "World Generator";
        }

        protected override void Initialize(ParametersPanel panel)
        {
            WorldGenerator generator = new();
            _generator = new GeneratorAdapter(generator);
            _generator.OnGenerationComplete += _renderMap;
            _gridLevel = new ParameterArray("Grid level", _generator.GridLevels - 1, Enumerable.Range(0, _generator.GridLevels).Cast<object>());

            _mapSettings.Add(_gridLevel);
            _mapSettings.RegisterProvider(panel);

            _generationSettings.Add(_regenerate);
            _generationSettings.RegisterProvider(panel);

            panel.RegisterParameter(generator.SeaToLand);
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
