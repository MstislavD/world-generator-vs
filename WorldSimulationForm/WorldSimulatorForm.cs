using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using WorldSimulation;
using WorldSimulation.HistorySimulation;
using Utilities;
using System.Reflection.Metadata.Ecma335;
using System.CodeDom;
using WorldSimulationForm.Tests;

namespace WorldSimulationForm
{
    public enum MapMode { Elevation, Height, Temperature, Precipitation, Biomes, Pops, Cells, Landmasses }

    [DesignerCategory("")]
    public partial class WorldSimulatorForm : Form
    {
        float _panelWidth = 0.05f;
        int _margin = 5;
        int _seed;

        bool _trackedEvents = true;
        bool _newHighlight = false;

        WorldGenerator _generator;

        Button _btnNextEvent;
        Point _mouse;
        Label _lblInfo;

        Bitmap? _image, _testImage;

        float _multiplier = 0;
        Vector2 _origin = new(0, 0);
        LogForm _logForm;
        bool _printLog = false;
        PaediaForm _paediaForm;

        WorldSimulation.Region? _highlightedRegion;
        List<WorldSimulation.Region> _highlightedArea = [];
        HistoricEvent? _currentEvent;

        ParameterArray _gridLevel;
        ParameterEnum<MapMode> _mapMode = new("Map mode", MapMode.Biomes);
        Parameter<bool> _regionBorder = new("Region borders", false);
        Parameter<bool> _subregionBorder = new("SRegion borders", false);
        ParameterArray _texture = new("Texture", "Texture", ["Color", "Texture", "Texture Imp"]);
        Parameter<bool> _regenerate = new("New seed", true);

        ParameterList _mapSettings = new ParameterList();
        ParameterList _generationSettings = new ParameterList();

        public WorldSimulatorForm()
        {
            DoubleBuffered = true;
            Visible = true;
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            Text = "World Simulator";

            _logForm = new LogForm();
            _paediaForm = new PaediaForm();
            _paediaForm.RaceHoverBegin += RaceHoverBegin;
            _paediaForm.RegionHoverBegin += RegionHoverBegin;

            _generator = new WorldGenerator();
            _generator.LogUpdated += Generator_LogUpdated;
            _generator.OnGenerationComplete += (sender, e) => { _renderMap(this, EventArgs.Empty); };

            ParametersPanel panel = new ParametersPanel();
            panel.Location = new Point(_margin);
            panel.Width = (int)(_panelWidth * ClientSize.Width);
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.OnParameterUpdate += Panel_OnParameterUpdate;
            Controls.Add(panel);

            Button btnStart = panel.AddButton("Start");
            btnStart.Click += BtnStart_Click;

            _gridLevel = new ParameterArray("Grid level", _generator.GridLevels, Enumerable.Range(0, _generator.GridLevels + 1).Cast<object>());

            //_mapSettings.Add(_gridLevel);
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

            Button btnTest = panel.AddButton("Test");
            //btnTest.Click += (s, e) => new PointLocationForm.PointLocationForm(_generator.SubregionGraph).Visible = true;
            //btnTest.Click += (s, e) => { _testImage = RaycastTest.GetImage((int)(ClientSize.Height * 0.5f)); Invalidate(); };
            //btnTest.Click += (s, e) => { _testImage = SpatialIndexTest.GetImage(_generator, imageMaxSize()); Invalidate(); };
            btnTest.Click += (s, e) => { _testImage = LayerGridTest.GetImage(imageMaxSize()); Invalidate(); };


            _lblInfo = panel.AddLabel("Info");
            _lblInfo.AutoSize = true;
            _lblInfo.MaximumSize = new Size(panel.Width - _lblInfo.Margin.Left * 2, 1000);

            MouseMove += WorldSimulatorForm_MouseMove;
            MouseClick += WorldSimulatorForm_MouseClick;          
            KeyDown += WorldSimulatorForm_KeyDown;
        }

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

        private void RegionHoverBegin(object? sender, WorldSimulation.Region? region)
        {
            _highlightedRegion = region;
            _newHighlight = true;
            Invalidate();
        }

        private void RaceHoverBegin(object? sender, Race race)
        {
            _highlightedArea = _generator.RegionMap.Regions.Where(r => r.Pops.Any(p => p.Race == race)).ToList();
            _highlightedRegion = null;
            _newHighlight = true;
            Invalidate();
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            _seed = new Random().Next();
            _logForm.Clear();

            Debug.WriteLine($"Generation seed: {_seed}");
            _generator.Regenerate(_seed);
            _paediaForm.InitializeHistory(_generator);
            _generator.History.EventLogged += History_EventLogged;

            _renderMap(sender, e);
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

        private void WorldSimulatorForm_MouseClick(object? sender, MouseEventArgs e)
        {
            WorldSubregionGraph graph = _generator.SubregionGraph;

            if (graph != null && _image != null &&
                _mouse.X >= _imageLeft && _mouse.X < _image.Width + _imageLeft &&
                _mouse.Y >= _margin && _mouse.Y < _image.Height + _margin)
            {
                double x = graph.Width * (_mouse.X - _imageLeft) / _image.Width;
                double y = graph.Height * (_mouse.Y - _margin) / _image.Height;

                if (_multiplier > 0)
                {
                    x = x / Math.Pow(2, _multiplier) + _origin.X;
                    y = y / Math.Pow(2, _multiplier) + _origin.Y;
                }

                WorldSubregion? subregion =
                        graph.SpatialIndex.FindPolygonContainingPoint(x, y) ??
                        graph.SpatialIndex.FindPolygonContainingPoint(x + graph.Width, y) ??
                        graph.SpatialIndex.FindPolygonContainingPoint(x - graph.Width, y);

                if (subregion != null)
                {
                    WorldSimulation.Region region = _generator.RegionMap.GetRegion(subregion);

                    _paediaForm.OnRegionSelected(region);
                    if (!_paediaForm.Visible)
                        _paediaForm.Show();
                    _paediaForm.Focus();
                }
            }
        }

        private void WorldSimulatorForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Location != _mouse)
            {
                _mouse = e.Location;
                _lblInfo.Text = "";

                WorldSubregionGraph graph = _generator.SubregionGraph;

                // cursor is inside the map image
                if (graph != null && _image != null &&
                    _mouse.X >= _imageLeft && _mouse.X < _image.Width + _imageLeft &&
                    _mouse.Y >= _margin && _mouse.Y < _image.Height + _margin)
                {
                    double x = graph.Width * (_mouse.X - _imageLeft) / _image.Width;
                    double y = graph.Height * (_mouse.Y - _margin) / _image.Height;

                    if (_multiplier > 0)
                    {
                        x = x / Math.Pow(2, _multiplier) + _origin.X;
                        y = y / Math.Pow(2, _multiplier) + _origin.Y;
                    }

                    WorldSubregion? subregion =
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

        private int _imageLeft => (int)(ClientSize.Width * _panelWidth + _margin * 2);

        private void WorldSimulatorForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_generator.SubregionGraph == null) return;

            float xStep = (float)_generator.SubregionGraph.Width / MathF.Pow(2, _multiplier + 2);
            float yStep = (float)_generator.SubregionGraph.Height / MathF.Pow(2, _multiplier + 2);

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

        private void History_EventLogged(object? sender, HistoricEvent e)
        {
            string info = $"T{_generator.History.Turn}: {e.Info}";
            _logForm.AddEntry(info);
        }
    }
}
