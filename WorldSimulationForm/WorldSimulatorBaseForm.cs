using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using Utilities;
using WorldSimulation;
using WorldSimulation.HistorySimulation;
using WorldSimulationForm.Tests;

namespace WorldSimulationForm
{
    public enum MapMode { Elevation, Height, Temperature, Precipitation, Biomes, Pops, Cells, Landmasses }

    /// <summary>
    /// The common part of the simulator forms: window layout, parameters panel, map rendering, zoom and pan.
    /// A subclass plugs a concrete generator in via Initialize() and overrides the hooks for
    /// generator-specific behavior (see LegacyWorldSimulatorForm / NewWorldSimulatorForm).
    /// </summary>
    [DesignerCategory("")]
    public abstract partial class WorldSimulatorBaseForm : Form
    {
        float _panelWidth = 0.05f;
        int _margin = 5;
        int _seed;

        bool _newHighlight = false;

        // assigned by the subclass in Initialize()
        protected IGenerator _generator = null!;

        Point _mouse;
        Label _lblInfo;

        Bitmap? _image, _testImage;
        Rectangle _imageRect;

        float _multiplier = 0;
        Vector2 _origin = new(0, 0);

        WorldSimulation.Region? _highlightedRegion;
        List<WorldSimulation.Region> _highlightedArea = [];
        protected HistoricEvent? _currentEvent;

        // created by the subclass in Initialize() (the value range differs between generators)
        protected ParameterArray _gridLevel = null!;
        protected ParameterEnum<MapMode> _mapMode = new("Map mode", MapMode.Elevation);
        Parameter<bool> _regionBorder = new("Region borders", false);
        protected Parameter<bool> _subregionBorder = new("SRegion borders", false);
        protected ParameterArray _texture = new("Texture", "Texture", ["Color", "Texture", "Texture Imp"]);
        protected Parameter<bool> _regenerate = new("New seed", true);

        protected ParameterList _mapSettings = new ParameterList();
        protected ParameterList _generationSettings = new ParameterList();

        public WorldSimulatorBaseForm()
        {
            DoubleBuffered = true;
            Visible = true;
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            Text = "World Simulator";

            _imageRect = new Rectangle();
            _imageRect.Width = (int)(ClientSize.Width * (1 - _panelWidth) - _margin * 3);
            _imageRect.Height = ClientSize.Height - _margin * 2;
            _imageRect.Location = new Point((int)(ClientSize.Width * _panelWidth + _margin * 2), _margin);

            ParametersPanel panel = new ParametersPanel();
            panel.Location = new Point(_margin);
            panel.Width = (int)(_panelWidth * ClientSize.Width);
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.OnParameterUpdate += Panel_OnParameterUpdate;
            Controls.Add(panel);

            Button btnStart = panel.AddButton("Start");
            btnStart.Click += BtnStart_Click;

            Initialize(panel);

            Button btnTest = panel.AddButton("Test");
            //btnTest.Click += (s, e) => new PointLocationForm.PointLocationForm(_generator.SubregionGraph).Visible = true;
            //btnTest.Click += (s, e) => { _testImage = RaycastTest.GetImage((int)(ClientSize.Height * 0.5f)); Invalidate(); };
            //btnTest.Click += (s, e) => { _testImage = SpatialIndexTest.GetImage(_generator, _imageRect.Size); Invalidate(); };
            btnTest.Click += (s, e) => { _testImage = LayerGridTest.GetImage(_imageRect.Size); Invalidate(); };

            MouseMove += WorldSimulatorBaseForm_MouseMove;
            MouseClick += WorldSimulatorBaseForm_MouseClick;
            KeyDown += WorldSimulatorBaseForm_KeyDown;

            _lblInfo = panel.AddLabel("Info");
            _lblInfo.AutoSize = true;
            _lblInfo.MaximumSize = new Size(panel.Width - _lblInfo.Margin.Left * 2, 1000);
        }

        /// <summary>Plugs in the generator: creates it, registers its parameters and buttons, subscribes to OnGenerationComplete.</summary>
        protected abstract void Initialize(ParametersPanel panel);

        // ----- subclass hooks -----

        /// <summary>Called after Start is pressed and a new world generated. Default: nothing.</summary>
        protected virtual void OnGenerationStarted() { }

        /// <summary>Called on every map (re)render, before the image is invalidated. Default: nothing.</summary>
        protected virtual void OnWorldRendered() { }

        /// <summary>The subregion graph used for mouse hit-testing and keyboard panning; null if the generator has none.</summary>
        protected virtual SubregionGraph? MapGraph => null;

        /// <summary>Called when the user clicks a subregion on the map. Default: nothing.</summary>
        protected virtual void OnSubregionClicked(WorldSimulation.Region region) { }

        /// <summary>The world size in world units (for keyboard pan steps); (0, 0) until a world exists.</summary>
        protected virtual (double Width, double Height) WorldSize() => (0, 0);

        /// <summary>Opens the sibling form for the other generator (this one stays hidden). Default: nothing.</summary>
        protected virtual void SwitchToSibling() { }

        private void Panel_OnParameterUpdate(object? sender, Parameter parameter)
        {
            if (_generationSettings.Contains(parameter))
                return;
            else if (_mapSettings.Contains(parameter))
                _renderMap(sender, EventArgs.Empty);
            else if (_regenerate) 
                _generator.Regenerate();
            else 
                _generator.Generate();
        }

        protected void RegionHoverBegin(object? sender, WorldSimulation.Region? region)
        {
            _highlightedRegion = region;
            if (region == null)
                _highlightedArea = []; // no subregion hovered — drop any stale race highlight from the paedia
            _newHighlight = true;
            Invalidate();
        }

        protected void RaceHoverBegin(object? sender, Race race)
        {
            _highlightedArea = _generator.RegionMap.Regions.Where(r => r.Pops.Any(p => p.Race == race)).ToList();
            _highlightedRegion = null;
            _newHighlight = true;
            Invalidate();
        }

        protected void RaceHoverEnd(object? sender, Race race)
        {
            _highlightedArea = []; // paedia left the race view / a label — drop its highlight
            _newHighlight = true;
            Invalidate();
        }

        protected void RegionHoverEnd(object? sender, WorldSimulation.Region region)
        {
            _highlightedRegion = null; // paedia left the region view / was hidden — drop its highlight
            _newHighlight = true;
            Invalidate();
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            _seed = new Random().Next();           
            Debug.WriteLine($"Generation seed: {_seed}");
            _generator.Regenerate(_seed);

            OnGenerationStarted();

            _renderMap(sender, e);
        }

        private void WorldSimulatorBaseForm_MouseClick(object? sender, MouseEventArgs e)
        {
            SubregionGraph? graph = MapGraph;

            if (graph != null && _image != null &&
                e.Location.X >= _imageRect.Left && e.Location.X < _image.Width + _imageRect.Left &&
                e.Location.Y >= _margin && e.Location.Y < _image.Height + _margin)
            {
                double x = graph.Width * (e.Location.X - _imageRect.Left) / _image.Width;
                double y = graph.Height * (e.Location.Y - _margin) / _image.Height;

                if (_multiplier > 0)
                {
                    x = x / Math.Pow(2, _multiplier) + _origin.X;
                    y = y / Math.Pow(2, _multiplier) + _origin.Y;
                }

                Subregion? subregion =
                        graph.SpatialIndex.FindPolygonContainingPoint(x, y) ??
                        graph.SpatialIndex.FindPolygonContainingPoint(x + graph.Width, y) ??
                        graph.SpatialIndex.FindPolygonContainingPoint(x - graph.Width, y);

                if (subregion != null)
                    OnSubregionClicked(_generator.RegionMap.GetRegion(subregion));
            }
        }

        private void WorldSimulatorBaseForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Location != _mouse)
            {
                _mouse = e.Location;
                _lblInfo.Text = "";

                SubregionGraph? graph = MapGraph;

                // cursor is inside the map image
                if (graph != null && _image != null &&
                    _mouse.X >= _imageRect.Left && _mouse.X < _image.Width + _imageRect.Left &&
                    _mouse.Y >= _margin && _mouse.Y < _image.Height + _margin)
                {
                    double x = graph.Width * (_mouse.X - _imageRect.Left) / _image.Width;
                    double y = graph.Height * (_mouse.Y - _margin) / _image.Height;

                    if (_multiplier > 0)
                    {
                        x = x / Math.Pow(2, _multiplier) + _origin.X;
                        y = y / Math.Pow(2, _multiplier) + _origin.Y;
                    }

                    Subregion? subregion =
                        graph.SpatialIndex.FindPolygonContainingPoint(x, y) ??
                        graph.SpatialIndex.FindPolygonContainingPoint(x + graph.Width, y) ??
                        graph.SpatialIndex.FindPolygonContainingPoint(x - graph.Width, y);

                    // cursor points to a subregion
                    if (subregion != null)
                    {
                        WorldSimulation.Region region = _generator.RegionMap.GetRegion(subregion);

                        // cursor moved to new subregion
                        if (!region.Equals(_highlightedRegion))
                            RegionHoverBegin(sender, region);
                    }
                    // cursor doesn't point to a subregion
                    else if (_highlightedRegion != null)
                    {
                        RegionHoverBegin(sender, null);
                    }

                    _lblInfo.Text = $"X: {x:F2}\nY: {y:F2}{_regionInfo(_highlightedRegion)}";
                }
            }
        }

        private string _regionInfo(WorldSimulation.Region? region)
        {
            string regionInfo = "";

            if (region == null) return "";

            regionInfo = $"\n{region.Name}";
            regionInfo += $"\n{region.Biome}";
            regionInfo += $"\nSize: {region.Size}";
            regionInfo += $"\nHeight: {region.Height:F1}m";
            regionInfo += !region.IsSea ? $"\nWater: {region.Water:F2}" : "";
            regionInfo += string.Concat(region.Traits.Select(t => $"\n{t.Name}"));

            List<Population> pops = region.Pops.ToList();
            if (pops.Count > 0)
            {
                regionInfo += $"\nPops ({pops.Count}):";
                regionInfo += string.Concat(pops.Select(p => $"\n{p.Race.Name}"));
            }

            return regionInfo;
        }

        private void WorldSimulatorBaseForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                SwitchToSibling();
                return;
            }

            (double w, double h) = WorldSize();
            if (w <= 0 || h <= 0) return;

            float xStep = (float)w / MathF.Pow(2, _multiplier + 2);
            float yStep = (float)h / MathF.Pow(2, _multiplier + 2);

            if (e.KeyCode == Keys.Add)
            {
                _multiplier += 1;
                _renderMap(sender, e);
            }
            else if (e.KeyCode == Keys.Subtract)
            {
                _multiplier -= 1;
                _renderMap(sender, e);
            }
            else if (e.KeyCode == Keys.Home)
            {
                _multiplier = 0;
                _origin = new Vector2(0, 0);
                _renderMap(sender, e);
            }      
            else if (e.KeyCode == Keys.D)
            {
                _origin = new(_origin.X + xStep, _origin.Y);
                _renderMap(sender, e);
            }
            else if (e.KeyCode == Keys.A)
            {
                _origin = new(_origin.X - xStep, _origin.Y);
                _renderMap(sender, e);
            }
            else if (e.KeyCode == Keys.S)
            {
                _origin = new(_origin.X, _origin.Y + yStep);
                _renderMap(sender, e);
            }
            else if (e.KeyCode == Keys.W)
            {
                _origin = new(_origin.X, _origin.Y - yStep);
                _renderMap(sender, e);
            }
            else if (e.KeyCode == Keys.R)
            {
                BtnStart_Click(sender, e);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // closing either simulator window ends the app: the other form, if open, is hidden
            // (F12 always hides the current one), so keeping the message loop alive would leave
            // the process running with no visible window — a non-interactive hang.
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image?.Dispose();
                _testImage?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
