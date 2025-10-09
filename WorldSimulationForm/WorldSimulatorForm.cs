using Parameters;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Numerics;
using WorldSimulation;
using WorldSimulation.HistorySimulation;

namespace WorldSimulationForm
{
    public enum MapMode { Elevation, Height, Temperature, Precipitation, Biomes, Pops, Cells, Landmasses }

    [DesignerCategory("")]
    public partial class WorldSimulatorForm : Form
    {
        float _panelWidth = 0.05f;
        int _margin = 5;
        int _mountainSeed;

        bool _trackedEvents = true;
        bool _newHighlight = false;

        WorldGenerator _generator;

        Button _btnNextEvent;
        ComboBox _cmbGridLevel;
        ComboBox _cmbMapMode, _cmbTexture;
        CheckBox _chbRegionBorder, _chbSubregionBorder;
        Point _mouse;
        Label _lblInfo;

        Bitmap? _image;

        float _multiplier = 0;
        Vector2 _origin = new(0, 0);
        LogForm _logForm;
        bool _printLog = false;
        PaediaForm _paediaForm;

        WorldSimulation.Region? _highlightedRegion;
        List<WorldSimulation.Region> _highlightedArea = [];
        HistoricEvent? _currentEvent;

        public WorldSimulatorForm()
        {
            DoubleBuffered = true;
            Visible = true;
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;

            _logForm = new LogForm();
            _paediaForm = new PaediaForm();
            _paediaForm.RaceHoverBegin += RaceHoverBegin;
            _paediaForm.RegionHoverBegin += RegionHoverBegin;

            ParametersPanel panel = new ParametersPanel();
            panel.Location = new Point(_margin);
            panel.Width = (int)(_panelWidth * ClientSize.Width);
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.TopDown;
            Controls.Add(panel);

            _generator = new WorldGenerator(panel);
            _generator.LogUpdated += _generator_LogUpdated;

            Button btnStart = _addButton(panel, "Start");
            btnStart.Click += _start;

            _cmbGridLevel = _addComboBox(panel, Enumerable.Range(0, _generator.GridLevels + 1), _generator.GridLevels);
            _cmbMapMode = _addComboBox(panel, Enum.GetValues(typeof(MapMode)).Cast<MapMode>(), MapMode.Biomes);

            _chbRegionBorder = _addCheckBox(panel, "Region Borders");
            _chbSubregionBorder = _addCheckBox(panel, "SRegion Borders");

            _cmbTexture = _addComboBox(panel, ["Color", "Texture", "Texture Imp"], "Texture");
                        
            panel.AddParameterControls(_generator.Parameters);
            panel.OnParameterUpdate += (s, p) => { _generator.Generate(); _renderMap(this, EventArgs.Empty); };

            Button btnLog = _addButton(panel, "Log");
            btnLog.Click += BtnLog_Click;

            Button btnPaedia = _addButton(panel, "Paedia");
            btnPaedia.Click += BtnPaedia_Click;

            _btnNextEvent = _addButton(panel, "Next Event");
            _btnNextEvent.Enabled = false;
            _btnNextEvent.Click += BtnNextEvent_Click;

            Button btnLocatorForm = _addButton(panel, "Locator Test");
            btnLocatorForm.Click += (s, e) => new PointLocationForm.Form1(_generator.SubregionGraph).Visible = true;

            _lblInfo = new Label();
            _lblInfo.AutoSize = true;
            _lblInfo.MaximumSize = new Size(panel.Width - _lblInfo.Margin.Left * 2, 1000);
            panel.Controls.Add(_lblInfo);

            MouseMove += WorldSimulatorForm_MouseMove;
            MouseClick += WorldSimulatorForm_MouseClick;          
            KeyDown += WorldSimulatorForm_KeyDown;
        }

        private Button _addButton(FlowLayoutPanel panel, string text)
        {
            Button btn = new Button() { Text = text };
            btn.Size = new Size(panel.ClientSize.Width - btn.Margin.All * 2, panel.ClientSize.Width / 3);
            panel.Controls.Add(btn);
            return btn;
        }

        private CheckBox _addCheckBox(FlowLayoutPanel panel, string name)
        {
            CheckBox chb = new CheckBox();
            chb.CheckedChanged += _renderMap;
            chb.Text = name;
            chb.Width = panel.Width - chb.Margin.Left * 2;
            chb.Height = chb.Width / 3;
            panel.Controls.Add(chb);
            return chb;
        }

        private ComboBox _addComboBox<T>(FlowLayoutPanel panel, IEnumerable<T> items, T item)
        {
            ComboBox cmb = new ComboBox();
            cmb.Items.AddRange(items.Cast<object>().ToArray());
            cmb.SelectedItem = item;
            cmb.SelectedValueChanged += _renderMap;
            cmb.Width = panel.Width - cmb.Margin.Left * 2;
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(cmb);
            return cmb;
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

        private void _generator_LogUpdated(object? sender, string entry)
        {
            if (_printLog && sender is HistorySimulator)
            {
                _logForm.AddEntry(entry);
            }               
        }

        private void WorldSimulatorForm_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_image != null && _mouse.X >= _imageLeft && _mouse.X < _image.Width + _imageLeft && _mouse.Y >= _margin && _mouse.Y < _image.Height + _margin)
            {
                double x = _generator.SubregionGraph.Width * (_mouse.X - _imageLeft) / _image.Width;
                double y = _generator.SubregionGraph.Height * (_mouse.Y - _margin) / _image.Height;

                if (_multiplier > 0)
                {
                    x = x / Math.Pow(2, _multiplier) + _origin.X;
                    y = y / Math.Pow(2, _multiplier) + _origin.Y;
                }

                Subregion subregion = _generator.SubregionGraph.Locator.GetRegion(x, y);

                if (subregion!= null)
                {
                    //string strVertices = "";
                    //foreach (Vertex v in subregion.Vertices)
                    //{
                    //    Vertex vv = new Vertex(Math.Round(v.X, 5), Math.Round(v.Y, 5)); 
                    //    strVertices += vv.ToString() + " - ";
                    //}
                    //Console.WriteLine($"Subregion vertices:{strVertices}");
                    //Console.WriteLine($"Cell: {subregion.Cell.GridPositionX}, {subregion.Cell.GridPositionY}");

                    WorldSimulation.Region region = _generator.RegionMap.GetRegion(subregion);
                  
                    _paediaForm.OnRegionSelected(region);
                    if (!_paediaForm.Visible)
                        _paediaForm.Show();
                    _paediaForm.Focus();
                }               

                //Console.WriteLine($"Last trapezoid: {_generator.SubregionGraph.Locator.LastTrapezoid}");
            }
        }

        private void WorldSimulatorForm_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Location != _mouse)
            {
                _mouse = e.Location;
                _lblInfo.Text = "";

                SubregionGraph graph = _generator.SubregionGraph;

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

                    Subregion subregion = 
                        graph.Locator.GetRegion(x, y) ??
                        graph.Locator.GetRegion(x + graph.Width, y) ??
                        graph.Locator.GetRegion(x - graph.Width, y);

                    // cursor points to a subregion
                    if (subregion != null && _cmbGridLevel.SelectedItem != null)
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
                _start(sender, e);
            }
        }

        void _start(object? sender, EventArgs e)
        {
            _mountainSeed = new Random().Next();
            _logForm.Clear();

            Console.WriteLine($"Generation seed: {_mountainSeed}");
            _generator.Regenerate(_mountainSeed);
            _paediaForm.InitializeHistory(_generator);
            _generator.History.EventLogged += History_EventLogged;

            _renderMap(sender, e);
        }

        private void History_EventLogged(object? sender, HistoricEvent e)
        {
            string info = $"T{_generator.History.Turn}: {e.Info}";
            _logForm.AddEntry(info);
        }
    }
}
