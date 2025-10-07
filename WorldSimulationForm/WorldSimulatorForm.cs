using System.Data;
using Topology;
using RandomExtension;
using WorldSimulation;
using Utilities;
using Parameters;
using System.Collections.Concurrent;

namespace WorldSimulationForm
{
    using static WorldSimulationForm.Properties.Resources;

    enum MapMode { Elevation, Height, Temperature, Precipitation, Biomes, Pops, Cells, Landmasses }
    public partial class WorldSimulatorForm : ParameterForm
    {
        float _panelWidth = 0.05f;
        int _margin = 5;
        int _mountainSeed;
      
        WorldGenerator _generator;

        ComboBox _cmbGridLevel;
        ComboBox _cmbMapMode, _cmbBiomesMode;
        CheckBox _chbRegionBorder, _chbSubregionBorder;
        Point _mouse;
        Label _lblInfo;

        Bitmap? _image;

        double _multiplier = 0;
        Vector2 _origin = new Vector2(0, 0);
        LogForm _logForm;
        bool _printLog = false;
        PaediaForm _paediaForm;

        WorldSimulation.Region? _highlightedRegion;
        WorldSimulation.HistorySimulation.HistoricEvent? _currentEvent;

        public WorldSimulatorForm()
        {
            DoubleBuffered = true;
            Visible = true;
            WindowState = FormWindowState.Maximized;

            _generator = new WorldGenerator(this);
            _generator.LogUpdated += _generator_LogUpdated;

            _logForm = new LogForm();
            _paediaForm = new PaediaForm();
            _paediaForm.RaceHoverBegin += _paediaForm_RaceHoverBegin;
            _paediaForm.RegionHoverBegin += _paediaForm_RegionHoverBegin;
            
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Location = new Point(_margin);
            panel.Width = (int)(_panelWidth * ClientSize.Width);
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.TopDown;
            Controls.Add(panel);

            Button btnStart = new Button();
            btnStart.Text = "Start";
            btnStart.Click += _start;
            _setButtonSize(btnStart, panel);
            panel.Controls.Add(btnStart);

            _cmbGridLevel = new ComboBox();
            _cmbGridLevel.Items.AddRange(Enumerable.Range(0, _generator.GridLevels + 1).Cast<object>().ToArray());
            _cmbGridLevel.SelectedItem = _generator.GridLevels;
            _cmbGridLevel.SelectedValueChanged += (s, e) => CreateGraphics().Clear(BackColor); // move to the _redrawMap method?
            _cmbGridLevel.SelectedValueChanged += _redrawMap;
            _cmbGridLevel.Width = panel.Width - _cmbGridLevel.Margin.Left * 2;
            _cmbGridLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(_cmbGridLevel);

            _cmbMapMode = new ComboBox();
            _cmbMapMode.Items.AddRange(Enum.GetValues(typeof(MapMode)).Cast<object>().ToArray());
            _cmbMapMode.SelectedItem = MapMode.Biomes;
            _cmbMapMode.SelectedValueChanged += _redrawMap;
            _cmbMapMode.Width = panel.Width - _cmbMapMode.Margin.Left * 2;
            _cmbMapMode.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(_cmbMapMode);

            _chbRegionBorder = _addCheckBox("Region Borders", panel);
            _chbSubregionBorder = _addCheckBox("SRegion Borders", panel);

            _cmbBiomesMode = new ComboBox();
            _cmbBiomesMode.Items.Add("Color");
            _cmbBiomesMode.Items.Add("Texture");
            _cmbBiomesMode.Items.Add("Texture Imp");
            _cmbBiomesMode.SelectedItem = "Texture";
            _cmbBiomesMode.SelectedValueChanged += _redrawMap;
            _cmbBiomesMode.Width = panel.Width - _cmbBiomesMode.Margin.Left * 2;
            _cmbBiomesMode.DropDownStyle = ComboBoxStyle.DropDownList;
            panel.Controls.Add(_cmbBiomesMode);

            AddParameterControls(_generator.Parameters, panel);
            OnParameterUpdate += (s, p) => _generator.Generate();

            Button btnLog = new Button();
            btnLog.Text = "Log";
            btnLog.Click += BtnLog_Click;
            _setButtonSize(btnLog, panel);
            panel.Controls.Add(btnLog);

            Button btnPaedia = new Button();
            btnPaedia.Text = "Paedia";
            btnPaedia.Click += BtnPaedia_Click;
            _setButtonSize(btnPaedia, panel);
            panel.Controls.Add(btnPaedia);

            Button btnNextEvent = new Button();
            btnNextEvent.Text = "Next Event";
            _setButtonSize(btnNextEvent, panel);
            btnNextEvent.Click += BtnNextEvent_Click;
            _generator.GenerationComplete += (s, e) => btnNextEvent.Text = "Next Event";
            _generator.GenerationComplete += (s, e) => btnNextEvent.Enabled = true;
            _generator.GenerationComplete += (s, e) => _currentEvent = null;
            panel.Controls.Add(btnNextEvent);

            Button btnLocatorForm = new Button();
            btnLocatorForm.Text = "Locator Test";
            btnLocatorForm.Click += (s, e) => new PointLocationForm.Form1(_generator.SubregionGraph).Visible = true;
            _setButtonSize(btnLocatorForm, panel);
            panel.Controls.Add(btnLocatorForm);

            _lblInfo = new Label();
            _lblInfo.AutoSize = true;
            _lblInfo.MaximumSize = new Size(panel.Width - _lblInfo.Margin.Left * 2, 1000);
            panel.Controls.Add(_lblInfo);

            MouseMove += WorldSimulatorForm_MouseMove;
            MouseClick += WorldSimulatorForm_MouseClick;

            KeyPreview = true;

            KeyDown += _handleInput;

            _generator.GenerationComplete += _redrawMap;
        }

        private void _setButtonSize(Button btn, FlowLayoutPanel panel)
        {
            btn.Size = new Size(panel.ClientSize.Width - btn.Margin.All * 2, panel.ClientSize.Width / 3);
        }
            

        private void _paediaForm_RegionHoverBegin(object? sender, WorldSimulation.Region region)
        {
            HexGrid grid = _generator.GetGrid((int)_cmbGridLevel.SelectedItem);
            Bitmap overlay = HexGridRenderer.Render(grid, _image.Width, _image.Height, _regionOutline(region));

            Graphics g = CreateGraphics();
            g.DrawImage(_image, _imageLeft, _margin);
            g.DrawImage(overlay, _imageLeft, _margin);
        }

        private void _paediaForm_RaceHoverBegin(object? sender, WorldSimulation.HistorySimulation.Race race)
        {
            IEnumerable<WorldSimulation.Region> regions = _generator.RegionMap.Regions.Where(r => r.Pops.Any(p => p.Race == race));

            HexGrid grid = _generator.GetGrid((int)_cmbGridLevel.SelectedItem);
            Bitmap overlay = HexGridRenderer.Render(grid, _image.Width, _image.Height, _regionOutline(regions));

            Graphics g = CreateGraphics();
            g.DrawImage(_image, _imageLeft, _margin);
            g.DrawImage(overlay, _imageLeft, _margin);
        }

        private void BtnPaedia_Click(object sender, EventArgs e)
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

        private void _generator_LogUpdated(object? sender, string e)
        {
            if (_printLog && sender is WorldSimulation.HistorySimulation.HistorySimulator)
            {
                _logForm.AddEntry(e);
            }               
        }

        private void WorldSimulatorForm_MouseClick(object sender, MouseEventArgs e)
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

        private void WorldSimulatorForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Location != _mouse)
            {
                _mouse = e.Location;

                if (_image != null && _mouse.X >= _imageLeft && _mouse.X < _image.Width + _imageLeft && _mouse.Y >= _margin && _mouse.Y < _image.Height + _margin)
                {
                    double x = _generator.SubregionGraph.Width * (_mouse.X - _imageLeft) / _image.Width;
                    double y = _generator.SubregionGraph.Height * (_mouse.Y - _margin) / _image.Height;

                    if (_multiplier > 0)
                    {
                        x = x / Math.Pow(2, _multiplier) + _origin.X;
                        y = y / Math.Pow(2, _multiplier) + _origin.Y;
                    }

                    Subregion subregion = _generator.SubregionGraph.Locator?.GetRegion(x, y);
                                        
                    if (subregion == null)
                    {
                        double width = _generator.SubregionGraph.Width;
                        subregion = _generator.SubregionGraph.Locator?.GetRegion(x + width, y);
                        if (subregion == null)
                        {
                            subregion = _generator.SubregionGraph.Locator?.GetRegion(x - width, y);
                        }
                    }

                    string regionInfo = "";

                    if (subregion != null)
                    {
                        WorldSimulation.Region region = _generator.RegionMap.GetRegion(subregion);

                        if (!region.Equals(_highlightedRegion))
                        {
                            _highlightedRegion = region;
                            HexGrid grid = _generator.GetGrid((int)_cmbGridLevel.SelectedItem);
                            Bitmap overlay = HexGridRenderer.Render(grid, _image.Width, _image.Height, _regionOutline(region));

                            Graphics g = CreateGraphics();
                            g.DrawImage(_image, _imageLeft, _margin);
                            g.DrawImage(overlay, _imageLeft, _margin);
                        }

                        regionInfo = $"\n{region.Name}";
                        regionInfo += $"\n{region.Biome}";
                        //regionInfo += $"\n{region.Belt}\n{region.Humidity}";
                        regionInfo += $"\nSize: {region.Size}\nHeight: {region.Height:F1}m";
                        if (!region.IsSea)
                        {
                            regionInfo += $"\nWater: {region.Water:F2}";
                        }
                        //if (region.River != null)
                        //{
                        //    regionInfo += $"\nRiver";
                        //}
                        foreach(WorldSimulation.HistorySimulation.RegionTrait trait in region.Traits)
                        {
                            regionInfo += $"\n{trait.Name}";
                        }

                        List<WorldSimulation.HistorySimulation.Population> pops = region.Pops.ToList();
                        if (pops.Count > 0)
                        {
                            regionInfo += $"\nPops ({pops.Count}):";
                            foreach(WorldSimulation.HistorySimulation.Population pop in pops)
                            {
                                regionInfo += $"\n{pop.Race.Name}";
                            }
                        }

                    }
                    else if (_highlightedRegion != null)
                    {
                        _highlightedRegion = null;
                        Graphics g = CreateGraphics();
                        g.DrawImage(_image, _imageLeft, _margin);
                    }

                    _lblInfo.Text = $"X: {x:F2}\nY: {y:F2}{regionInfo}";
                }
                else
                {
                    _lblInfo.Text = "";
                }
            }
        }

        private int _imageLeft => (int)(ClientSize.Width * _panelWidth + _margin * 2);

        private CheckBox _addCheckBox(string name, FlowLayoutPanel panel)
        {
            CheckBox chb = new CheckBox();
            chb.CheckedChanged += _redrawMap;
            chb.Text = name;
            chb.Width = panel.Width - chb.Margin.Left * 2;
            chb.Height = chb.Width / 3;
            panel.Controls.Add(chb);
            return chb;
        }

        private void _handleInput(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Add)
            {
                _multiplier += 1;
                _redrawMap(sender, e);
            }
            else if (e.KeyCode == Keys.Subtract)
            {
                _multiplier -= 1;
                _redrawMap(sender, e);
            }
            else if (e.KeyCode == Keys.Home)
            {
                _multiplier = 0;
                _origin = new Vector2(0, 0);
                _redrawMap(sender, e);
            }      
            else if (e.KeyCode == Keys.D)
            {
                double dx = _generator.SubregionGraph.Width / Math.Pow(2, _multiplier + 2);
                _origin = new(_origin.X + dx, _origin.Y);
                _redrawMap(sender, e);
            }
            else if (e.KeyCode == Keys.A)
            {
                double dx = -_generator.SubregionGraph.Width / Math.Pow(2, _multiplier + 2);
                _origin = new(_origin.X + dx, _origin.Y);
                _redrawMap(sender, e);
            }
            else if (e.KeyCode == Keys.S)
            {
                double dy = _generator.SubregionGraph.Height / Math.Pow(2, _multiplier + 2);
                _origin = new(_origin.X, _origin.Y + dy);
                _redrawMap(sender, e);
            }
            else if (e.KeyCode == Keys.W)
            {
                double dy = -_generator.SubregionGraph.Height / Math.Pow(2, _multiplier + 2);
                _origin = new(_origin.X, _origin.Y + dy);
                _redrawMap(sender, e);
            }
            else if (e.KeyCode == Keys.R)
            {
                _start(sender, e);
            }
        }

        protected override bool IsSeedParameter(Parameter parameter) => _generator.IsSeedParameter(parameter);

        private void _redrawMap(object? sender, EventArgs e)
        {
            if (!_generator.GenerationIsComplete)
            {
                return;
            }         

            Graphics g = CreateGraphics();

            int gridLevel = (int)_cmbGridLevel.SelectedItem;

            HexGrid grid = _generator.GetGrid(gridLevel);

            RenderObjects objects = new RenderObjects();
            if (_cmbMapMode.SelectedItem.Equals(MapMode.Elevation))
            {
                objects = _elevationImage(grid);
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Height))
            {
                objects = _heightImage(grid);
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Temperature))
            {
                objects = _temperatureImage();
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Precipitation))
            {
                objects = _precipitationImage();
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Biomes))
            {
                objects = _biomesImage();
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Pops))
            {
                objects = _popImage();
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Cells))
            {
                objects = _cellsImage();
            }
            else if (_cmbMapMode.SelectedItem.Equals(MapMode.Landmasses))
            {
                objects = _landmassImage();
            }

            if (_multiplier != 0)
            {
                objects.Multiplier = Math.Pow(2, _multiplier);
                objects.Origin = _origin;
            }

            int imageMaxWidth = (int)(ClientSize.Width * (1- _panelWidth) - _margin * 3);
            int imageMaxHeight = ClientSize.Height - _margin * 2;
            _image = HexGridRenderer.Render(grid, imageMaxWidth, imageMaxHeight, objects);

            g.DrawImage(_image, _imageLeft, _margin);
        }

        private RenderObjects _elevationImage(HexGrid grid)
        {
            Dictionary<Elevation, Brush> brushByElevation = new Dictionary<Elevation, Brush>();
            brushByElevation[Elevation.DeepOcean] =  Brushes.MediumBlue;
            brushByElevation[Elevation.ShallowOcean] = Brushes.Blue;
            brushByElevation[Elevation.Lowland] = Brushes.Green;
            brushByElevation[Elevation.Upland] = Brushes.Yellow;
            brushByElevation[Elevation.Highland] = Brushes.Orange;
            brushByElevation[Elevation.Mountain] = Brushes.Brown;

            Pen ridgePen = new Pen(Color.DarkRed, 0);
            ridgePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            ridgePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            RenderObjects objects = new RenderObjects();
            objects.Polygons.AddRange(grid.Cells.Select(c => new PolygonData(c, brushByElevation[_generator.GetData(c).Elevation])));
            IEnumerable<Edge> edges = _chbRegionBorder.Checked ? grid.Edges.Where(_generator.RegionBorder) : grid.Edges.Where(_generator.IsShore);
            objects.Segments.AddRange(edges.Select(e => new SegmentData(e, Pens.Black)));
            objects.Segments.AddRange(grid.Edges.Where(_generator.HasRidge).Select(e => new SegmentData(e, ridgePen)));

            return objects;
        }

        private RenderObjects _heightImage(HexGrid grid)
        {            
            Color lowlandColor = Color.Green;
            Color uplandColor = Color.Yellow;
            Color highlandColor = Color.Orange;
            Color mountainColor = Color.Brown;
            Color ridgeColor = Color.DarkRed;
            Color shallowColor = Color.Blue;
            Color deepColor = Color.MediumBlue;
            Color deepestColor = Color.DarkBlue;

            Pen ridgePen = new Pen(Color.DarkRed, 0);
            ridgePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            ridgePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            Dictionary<double, Color> colorByHeight = new Dictionary<double, Color>();
            int uplandHeight = _generator.GetCountByElevation(Elevation.Lowland);
            int highlandHeight = _generator.GetCountByElevation(Elevation.Upland) + uplandHeight;
            int mountainHeight = _generator.GetCountByElevation(Elevation.Highland) + highlandHeight;
            colorByHeight[0] = lowlandColor;
            colorByHeight[uplandHeight] = uplandColor;
            colorByHeight[highlandHeight] = highlandColor;
            colorByHeight[mountainHeight] = mountainColor;

            Dictionary<double, Color> colorByDepth = new Dictionary<double, Color>();
            int deepHeight = _generator.GetCountByElevation(Elevation.ShallowOcean);
            int deepestHeight = _generator.GetCountByElevation(Elevation.DeepOcean) + deepHeight;
            colorByDepth[0] = shallowColor;
            colorByDepth[deepHeight] = deepColor;
            colorByDepth[deepestHeight] = deepestColor;

            VectorConverter<Color> converter = new ColorConverter();

            RenderObjects objects = new RenderObjects();

            Dictionary<HexCell, Color> colorByCell = new Dictionary<HexCell, Color>();
            foreach (HexCell cell in grid.Cells)
            {
                if (_generator.IsSea(cell))
                {
                    double height = -_generator.GetHeight(cell);
                    colorByCell[cell] = height > 0 ? Interpolation.Interpolate(colorByDepth, height, converter) : Color.Blue;
                }
                else
                {
                    double height = _generator.GetHeight(cell);
                    colorByCell[cell] = height > 0 ? Interpolation.Interpolate(colorByHeight, height, converter) : Color.Green;
                }
            }

            if (_generator.LastGrid(grid))
            {
                return _subregionHeightImageRenderObjects(colorByCell, ridgeColor);
            }
            else
            {
                objects.Polygons.AddRange(grid.Cells.Select(c => new PolygonData(c, colorByCell[c])));
                IEnumerable<Edge> edges = _chbRegionBorder.Checked ? grid.Edges.Where(_generator.RegionBorder) : grid.Edges.Where(_generator.IsShore);
                objects.Segments.AddRange(edges.Select(e => new SegmentData(e, Pens.Black)));
                objects.Segments.AddRange(grid.Edges.Where(_generator.HasRidge).Select(e => new SegmentData(e, ridgePen)));

                IEnumerable<Vector2[]> cellRivers = grid.Cells.Where(_generator.IsLand).Where(_generator.HasRiver).Select(c => new Vector2[] { c.Center, _generator.GetDrainage(c).Center });
                objects.Segments.AddRange(cellRivers.Select(s => new SegmentData(s, Color.Blue)));

                IEnumerable<Vector2[]> edgeRivers = grid.Edges.Where(_generator.HasRidge).Where(_generator.HasRiver).Select(e => new Vector2[] { e.Center, _generator.GetDrainage(e).Center });
                objects.Segments.AddRange(edgeRivers.Select(s => new SegmentData(s, Color.Blue)));
            }

            return objects;
        }

        private RenderObjects _temperatureImage()
        {
            Dictionary<Belt, Brush> brushByBelt = new Dictionary<Belt, Brush>();
            brushByBelt[Belt.Polar] = Brushes.Purple;
            brushByBelt[Belt.Boreal] = Brushes.Teal;
            brushByBelt[Belt.Temperate] = Brushes.Green;
            brushByBelt[Belt.Subtropical] = Brushes.Olive;
            brushByBelt[Belt.Tropical] = Brushes.Yellow;

            Dictionary<double, Color> colorByTemperature = _rainbowColors();

            RenderObjects objects = new RenderObjects();

            bool drawBelts = false; // _chbBelts.Checked;

            ColorConverter converter = new ColorConverter();
            Func<Subregion, Brush> colorBySubregion = s =>
            {
                Brush color = drawBelts ? brushByBelt[s.Region.Belt] : new SolidBrush(Interpolation.Interpolate(colorByTemperature, s.Region.Temperature, converter));

                if (_generator.IsSea(s))
                    color = Brushes.Blue;// new SolidBrush(Color.FromArgb(75, (color as SolidBrush).Color));
                if (_generator.HasRidge(s))
                    color = Brushes.DarkRed;

                return color;
            };

            objects.Polygons.AddRange(_generator.SubregionGraph.Subregions.Select(s => new PolygonData(s.Vertices, colorBySubregion(s))));

            _addBordersToSubregionImage(objects);

            return objects;
        }

        private RenderObjects _precipitationImage()
        {
            RenderObjects objects = new RenderObjects();

            Dictionary<double, Color> colorByTemperature = new Dictionary<double, Color>();
            colorByTemperature[0] = Color.Red;
            colorByTemperature[1] = Color.Blue;

            bool drawZones = true; // _chbZones.Checked;

            Func<Subregion, Brush> brushBySubregion = s =>
            {
                Brush brush;
                if (drawZones)
                {
                    if (s.Region.Humidity == Humidity.Dry)
                        brush = Brushes.Red;
                    else if (s.Region.Humidity == Humidity.Seasonal)
                        brush = Brushes.Yellow;
                    else
                        brush = Brushes.Green;
                }
                else
                {
                    brush = Brushes.Green; // Interpolation.Interpolate(colorByTemperature, _generator.GetPrecipitation(s), ColorConverter.Converter);
                }

                if (s.Region.IsSea)
                    brush = Brushes.Blue; // Color.FromArgb(75, color);
                if (s.Region.IsRidge)
                    brush = Brushes.DarkRed;

                return brush;
            };

            objects.Polygons.AddRange(_generator.SubregionGraph.Subregions.Select(s => new PolygonData(s.Vertices, brushBySubregion(s))));

            _addBordersToSubregionImage(objects);

            return objects;
        }

        private RenderObjects _biomesImage()
        {
            RenderObjects objects = new RenderObjects();

            bool imp_textures = _cmbBiomesMode.SelectedItem.Equals("Texture Imp");

            Dictionary<Biome, TextureBrush> brushByBiome = new Dictionary<Biome, TextureBrush>();
            Dictionary<Biome, Brush> colorByBiome = new Dictionary<Biome, Brush>();

            brushByBiome[Biomes.WetTundra] = new TextureBrush(wet_tundra);
            brushByBiome[Biomes.Tundra] = new TextureBrush(tundra);
            brushByBiome[Biomes.DryTundra] = new TextureBrush(dry_tundra);
            brushByBiome[Biomes.WetTaiga] = new TextureBrush(wet_taiga);
            brushByBiome[Biomes.Taiga] = new TextureBrush(taiga);
            brushByBiome[Biomes.DryTaiga] = new TextureBrush(imp_textures ? dry_taiga_imp : dry_taiga);
            brushByBiome[Biomes.MixedForest] = new TextureBrush(mixed_forest);
            brushByBiome[Biomes.ForestSteppe] = new TextureBrush(imp_textures ? forest_steppe_imp : forest_steppe);
            brushByBiome[Biomes.TemperateSteppe] = new TextureBrush(imp_textures ? temperate_steppe_imp : grassland);
            brushByBiome[Biomes.BroadleafForest] = new TextureBrush(broadleaf);
            brushByBiome[Biomes.SubtropicalSteppe] = new TextureBrush(imp_textures ? temperate_steppe_imp : grassland);
            brushByBiome[Biomes.SubtropicalDesert] = new TextureBrush(imp_textures ? semidesert_imp : semidesert);
            brushByBiome[Biomes.Rainforest] = new TextureBrush(imp_textures ? rainforest_imp : rainforest);
            brushByBiome[Biomes.Savanna] = new TextureBrush(imp_textures ? savanna_imp : savanna);
            brushByBiome[Biomes.TropicalDesert] = new TextureBrush(imp_textures ? desert_imp : desert);
            brushByBiome[Biomes.ShallowOcean] = new TextureBrush(imp_textures ? shallow_sea_imp : shallow_sea);
            brushByBiome[Biomes.DeepOcean] = new TextureBrush(imp_textures ? deep_sea_imp : deep_sea);
            brushByBiome[Biomes.PolarSea] = new TextureBrush(imp_textures ? deep_sea_imp : polar_sea);
            brushByBiome[Biomes.Mountains] = new TextureBrush(imp_textures ? mountains_imp : mountains);

            colorByBiome[Biomes.WetTundra] = Brushes.Cyan;
            colorByBiome[Biomes.Tundra] = Brushes.Cyan;
            colorByBiome[Biomes.DryTundra] = Brushes.Cyan;
            colorByBiome[Biomes.WetTaiga] = Brushes.Teal;
            colorByBiome[Biomes.Taiga] = Brushes.Teal;
            colorByBiome[Biomes.DryTaiga] = Brushes.Teal;
            colorByBiome[Biomes.MixedForest] = Brushes.LimeGreen;
            colorByBiome[Biomes.ForestSteppe] = Brushes.LimeGreen;
            colorByBiome[Biomes.TemperateSteppe] = Brushes.Yellow;
            colorByBiome[Biomes.BroadleafForest] = Brushes.LimeGreen;
            colorByBiome[Biomes.SubtropicalSteppe] = Brushes.Yellow;
            colorByBiome[Biomes.SubtropicalDesert] = Brushes.Orange;
            colorByBiome[Biomes.Rainforest] = Brushes.DarkGreen;
            colorByBiome[Biomes.Savanna] = Brushes.Olive; ;
            colorByBiome[Biomes.TropicalDesert] = Brushes.Orange;
            colorByBiome[Biomes.ShallowOcean] = Brushes.Blue;
            colorByBiome[Biomes.DeepOcean] = Brushes.Blue;
            colorByBiome[Biomes.PolarSea] = Brushes.Blue;
            colorByBiome[Biomes.Mountains] = Brushes.DarkRed;

            foreach (Subregion s in _generator.SubregionGraph.Subregions)
            {
                Biome biome = _generator.RegionMap.GetRegion(s).Biome;

                if (_cmbBiomesMode.SelectedItem.Equals("Color"))
                    objects.Polygons.Add(new PolygonData(s.Vertices, colorByBiome[biome]));
                else
                    objects.Polygons.Add(new PolygonData(s.Vertices, brushByBiome[biome]));               
            }

            if (_multiplier < 2 && !_cmbBiomesMode.SelectedItem.Equals("Color"))
            {
                RandomExt rnd = new RandomExt(_mountainSeed);

                List<Bitmap> mountains_mst = new List<Bitmap>() { mountain };
                List<Bitmap> mountains_imp =
                    new List<Bitmap>() { mountain_imp4, mountain_imp5, mountain_imp6 };

                List<Bitmap> mountains = imp_textures ? mountains_imp : mountains_mst;

                int count = mountains.Count;
                for (int i = 0; i < count; i++)
                {
                    mountains[i].MakeTransparent(Color.White);
                    Bitmap mirror = new Bitmap(mountains[i]);
                    mirror.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    mountains.Add(mirror);
                }

                float scale = imp_textures ? 0.75f : 1.0f;

                foreach (Subregion subregion in _generator.SubregionGraph.Subregions.Where(_generator.HasRidge).OrderBy(s => s.Center.Y))
                {                     
                    Bitmap mountain = rnd.NextItem(mountains);
                    float localScale = (float)(scale * (0.8 + 0.2 * rnd.NextDouble()));
                    objects.Images.Add(new ImageData(subregion.Center, mountain, localScale));
                }
            }

            if (_cmbBiomesMode.SelectedItem.Equals("Color"))
            {
                _addRivers(objects, Color.Blue, 3);
            }
            else
            {
                _addRivers(objects, Color.DarkBlue, 3);
                _addRivers(objects, Color.Blue, 1);
            }             

            _addBordersToSubregionImage(objects);

            return objects;
        }

        private RenderObjects _popImage()
        {
            RenderObjects objects = new RenderObjects();

            Dictionary<WorldSimulation.HistorySimulation.Race, Brush> brushByRace = new Dictionary<WorldSimulation.HistorySimulation.Race, Brush>();

            Func<WorldSimulation.HistorySimulation.Race, Brush> getBrush = (race) =>
            {
                if (!brushByRace.ContainsKey(race))
                {
                    int[] vector = new RandomExt(race.Seed).NextVector(3, 256);
                    brushByRace[race] = new SolidBrush(Color.FromArgb(vector[0], vector[1], vector[2]));
                }
                return brushByRace[race];
            };

            foreach (WorldSimulation.Region region in _generator.RegionMap.Regions)
            {
                List<Brush> brushes = new List<Brush>();
                if (!region.IsRidge && !region.IsSea)
                {
                    foreach (WorldSimulation.HistorySimulation.Population pop in region.Pops)
                    {
                        brushes.Add(getBrush(pop.Race));
                    }
                    if (brushes.Count == 0)
                        brushes.Add(Brushes.White);
                }
                else
                    brushes.Add(region.IsRidge ? Brushes.Black : Brushes.Blue);

                int i = 0;
                foreach (Subregion s in region.Subregions)
                {
                    if (i >= brushes.Count)
                        i = 0;

                    objects.Polygons.Add(new PolygonData(s.Vertices, brushes[i]));

                    i += 1;
                }
            }

            //foreach (Subregion s in _generator.SubregionGraph.Subregions)
            //{
            //    WorldSim.Region region = _generator.RegionMap.GetRegion(s);
            //    Brush brush;

            //    if (!region.IsRidge && !region.IsSea)
            //        brush = region.Pops.Count() > 0 ? getBrush(region.Pops.First().Race) : Brushes.White;
            //    else
            //        brush = region.IsRidge ? Brushes.Black : Brushes.Blue;                  

            //    objects.Polygons.Add(new PolygonData(s.Vertices, brush));
            //}

            _addRivers(objects, Color.Blue, 3);
            _addBordersToSubregionImage(objects);

            if (_currentEvent != null)
            {
                if (_currentEvent.Destination != null)
                {
                    Pen arrow = new Pen(Color.Black, 5);
                    arrow.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    arrow.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                    Vector2[] vertices = new Vector2[] { _currentEvent.Origin.Center, _currentEvent.Destination.Center };
                    objects.Segments.Add(new SegmentData(vertices, arrow));

                    arrow = new Pen(Color.Red, 3);
                    arrow.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    arrow.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                    objects.Segments.Add(new SegmentData(vertices, arrow));
                }
                else
                {
                    if (_currentEvent.Origin != null)
                    {
                        foreach (Subregion sreg in _currentEvent.Origin.Subregions)
                        {
                            foreach (SubregionEdge sedge in sreg.Edges.Where(sreg.HasNeighbor))
                            {
                                Subregion neighbor = sreg.GetNeighbor(sedge);
                                if (!sreg.SameRegion(neighbor))
                                    objects.Segments.Add(new SegmentData(sedge.Vertices.ToArray(), Color.Red, 5));
                            }
                        }
                    }                  
                }
            }

            return objects;
        }

        private RenderObjects _landmassImage()
        {
            RenderObjects objects = new RenderObjects();

            Dictionary<Landmass, Brush> brushByLandmass = new Dictionary<Landmass, Brush>();
            foreach(Landmass landmass in _generator.LandmassData.Landmasses)
            {
                int[] vector = new RandomExt(landmass.Seed).NextVector(3, 256);
                brushByLandmass[landmass] = new SolidBrush(Color.FromArgb(vector[0], vector[1], vector[2]));
            }

            Func<Subregion, Brush> getBrush = subregion =>
            {
                WorldSimulation.Region region = subregion.Region;
                if (region.IsSea)
                    return Brushes.Blue;
                else if (region.Biome == Biomes.Mountains)
                    return Brushes.Black;
                else
                    return brushByLandmass[region.Landmass];
            };

            objects.Polygons.AddRange(_generator.SubregionGraph.Subregions.Select(s => new PolygonData(s.Vertices, getBrush(s))));
            objects.Vertices.AddRange(_generator.LandmassData.Landmasses.Select(s => new VertexData(s.Center, Brushes.Black)));

            foreach(Landmass landmass in _generator.LandmassData.Landmasses)
            {
                foreach(Landmass neighbor in landmass.Neighbors)
                {
                    double x = neighbor.Center.X;
                    if (x - landmass.Center.X > _generator.SubregionGraph.Width / 2)
                        x -= _generator.SubregionGraph.Width;
                    else if (x - landmass.Center.X < -_generator.SubregionGraph.Width / 2)
                        x += _generator.SubregionGraph.Width;

                    objects.Segments.Add(new SegmentData([landmass.Center, new(x, neighbor.Center.Y)], Pens.Black));
                }
            }

            _addBordersToSubregionImage(objects);

            return objects;
        }

        private RenderObjects _cellsImage()
        {
            RenderObjects objects = new RenderObjects();
            RandomExt random = new RandomExt();
            SubregionGraph graph = _generator.SubregionGraph;

            List<SolidBrush> brushes = ((KnownColor[])Enum.GetValues(typeof(KnownColor))).Where(kc=>!kc.Equals(KnownColor.Transparent)).
                Select(kc=> new SolidBrush(Color.FromKnownColor(kc))).ToList();

            Dictionary<WorldSimulation.Region, SolidBrush> bbr = _generator.RegionMap.Regions.ToDictionary(r => r, r => random.NextItem(brushes));
            ConcurrentDictionary<WorldSimulation.Region, SolidBrush> brushByRegion = new ConcurrentDictionary<WorldSimulation.Region, SolidBrush>(bbr);

            int horRes = 1600;
            double cellSize = graph.Width / horRes;
            int verRes = (int)(graph.Height / cellSize);

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            Action<int> doColumn = i =>
            {
                for (int j = 0; j < verRes; j++)
                {
                    Vector2[] v = new Vector2[4];
                    v[0] = new Vector2(cellSize * i, cellSize * j);
                    v[1] = new Vector2(cellSize * (i + 1), cellSize * j);
                    v[2] = new Vector2(cellSize * (i + 1), cellSize * (j + 1));
                    v[3] = new Vector2(cellSize * i, cellSize * (j + 1));

                    Vector2 c = new Vector2(cellSize * (i + 0.5), cellSize * (j + 0.5));
                    Subregion subregion = graph.Locator.GetRegion(c.X, c.Y);

                    if (subregion == null)
                        subregion = graph.Locator.GetRegion(c.X + graph.Width, c.Y);

                    if (subregion == null)
                        subregion = graph.Locator.GetRegion(c.X - graph.Width, c.Y);

                    if (subregion != null)
                    {
                        WorldSimulation.Region region = subregion.Region;
                        lock (objects.Polygons)
                            objects.Polygons.Add(new PolygonData(v, brushByRegion[region]));
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            };

            Parallel.For(0, horRes, doColumn);
            //for (int i = 0; i < horRes; i++) doColumn(i);

            Console.WriteLine($"Cell map of size {horRes} x {verRes} built in {sw.ElapsedMilliseconds} ms");

            return objects;
        }

        private RenderObjects _subregionHeightImageRenderObjects(Dictionary<HexCell, Color> colorByCell, Color ridgeColor)
        {
            RenderObjects objects = new RenderObjects();
            SubregionGraph graph = _generator.SubregionGraph;

            foreach (Subregion subregion in graph.Subregions)
            {
                List<Vector2> polygon = subregion.Vertices.ToList();
                objects.Polygons.Add(new PolygonData(polygon, subregion.Type == SubregionType.Cell ? colorByCell[subregion.Cell] : ridgeColor));
            }

            _addRivers(objects, Color.Blue, 1);
            _addBordersToSubregionImage(objects);

            return objects;
        }

        private void _addRivers(RenderObjects objects, Color riverColor, int width)
        {
            SubregionGraph graph = _generator.SubregionGraph;
            Pen riverPen = new Pen(riverColor, width);
            riverPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            riverPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            foreach (Subregion subregion in graph.CellSubregions.Where(s => s.River))
            {
                SubregionEdge edge = subregion.GetEdge(subregion.Drainage);

                Vector2 v = Vector2.Lerp(subregion.Center, edge.Center, 0.5);

                Vector2[] vertices = new Vector2[] { v, edge.Center };
                objects.Segments.Add(new SegmentData(vertices, riverPen));

                bool riverOrigin = true;

                foreach (Subregion neighbor in subregion.Neighbors.Where(n => n.River && n.Drainage == subregion))
                {
                    riverOrigin = false;
                    Vector2 v1 = Vector2.Lerp(subregion.Center, subregion.GetEdge(neighbor).Center, 0.5);
                    vertices = new Vector2[] { v1, subregion.GetEdge(neighbor).Center };
                    objects.Segments.Add(new SegmentData(vertices, riverPen));
                    vertices = new Vector2[] { v1, v };
                    objects.Segments.Add(new SegmentData(vertices, riverPen));
                }

                if (riverOrigin)
                {
                    vertices = new Vector2[] { v, subregion.Center };
                    objects.Segments.Add(new SegmentData(vertices, riverPen));
                }
            }

            foreach (Subregion subregion in graph.EdgeSubregions.Where(s => s.River))
            {
                if (subregion.Drainage != null)
                {
                    SubregionEdge edge = subregion.GetEdge(subregion.Drainage);
                    Vector2[] vertices = new Vector2[] { subregion.Center, edge.Center };
                    objects.Segments.Add(new SegmentData(vertices, riverPen));
                }
            }
        }

        void _start(object? sender, EventArgs e)
        {
            _mountainSeed = new Random().Next();
            _logForm.Clear();
            //_generator.Regenerate();

            //_mountainSeed = 1611544660;
            Console.WriteLine($"Generation seed: {_mountainSeed}");
            _generator.Regenerate(_mountainSeed);
            _paediaForm.InitializeHistory(_generator);
            _generator.History.EventLogged += History_EventLogged;
        }

        private void History_EventLogged(object sender, EventArgs e)
        {
            var he = sender as WorldSimulation.HistorySimulation.HistoricEvent;
            string info = $"T{_generator.History.Turn}: {he.Info}";
            _logForm.AddEntry(info);
        }

        Dictionary<double, Color> _rainbowColors()
        {
            Dictionary<double, Color> rainbow = new Dictionary<double, Color>();
            double step = 1.0 / 6;
            rainbow[0 * step] = Color.DarkViolet;
            rainbow[1 * step] = Color.Blue;
            rainbow[2 * step] = Color.Cyan;
            rainbow[3 * step] = Color.Green;
            rainbow[4 * step] = Color.Yellow;
            rainbow[5 * step] = Color.Orange;
            rainbow[6 * step] = Color.Red;
            return rainbow;            
        }

        void _addBordersToSubregionImage(RenderObjects objects)
        {
            Color subregionBorderColor = Color.FromArgb(20, Color.Black);
            Pen subregionPen = new Pen(subregionBorderColor);
            Brush subregionBrush = new SolidBrush(subregionBorderColor);

            foreach (Subregion sreg in _generator.SubregionGraph.Subregions)
            {
                foreach (SubregionEdge sedge in sreg.Edges.Where(sreg.HasNeighbor))
                {
                    Subregion neighbor = sreg.GetNeighbor(sedge);
                    if (_chbRegionBorder.Checked && !sreg.SameRegion(neighbor))
                        objects.PreImageSegments.Add(new SegmentData(sedge.Vertices, Pens.Black));
                    else if (_generator.IsLand(sreg) && _generator.IsSea(neighbor))
                        objects.PreImageSegments.Add(new SegmentData(sedge.Vertices, Pens.Black));
                    else if (_chbSubregionBorder.Checked)
                    {
                        objects.PreImageSegments.Add(new SegmentData(sedge.Vertices, subregionPen));
                        if (_multiplier > 1)
                        {
                            foreach (Vector2 v in sedge.Vertices)
                            {
                                objects.Vertices.Add(new VertexData(v, subregionBrush));
                            }
                        }
                    }
                }

                if (_chbSubregionBorder.Checked && _multiplier > 1)
                {
                    objects.Vertices.Add(new VertexData(sreg.Center, subregionBrush));
                }
            }
        }

        private RenderObjects _regionOutline(WorldSimulation.Region region)
        {
            RenderObjects objects = new RenderObjects();

            if (_multiplier != 0)
            {
                objects.Multiplier = Math.Pow(2, _multiplier);
                objects.Origin = _origin;
            }

            foreach (Subregion sreg in region.Subregions)
            {
                foreach (SubregionEdge sedge in sreg.Edges.Where(sreg.HasNeighbor))
                {
                    Subregion neighbor = sreg.GetNeighbor(sedge);
                    if (!sreg.SameRegion(neighbor))
                        objects.Segments.Add(new SegmentData(sedge.Vertices.ToArray(), Color.Black, 3));
                }
            }

            return objects;
        }

        private RenderObjects _regionOutline(IEnumerable<WorldSimulation.Region> regions)
        {
            RenderObjects objects = new RenderObjects();
            HashSet<WorldSimulation.Region> _outlinedRegions = new HashSet<WorldSimulation.Region>(regions);

            if (_multiplier != 0)
            {
                objects.Multiplier = Math.Pow(2, _multiplier);
                objects.Origin = _origin;
            }

            foreach(WorldSimulation.Region region in regions)
            {
                foreach (Subregion sreg in region.Subregions)
                {
                    foreach (SubregionEdge sedge in sreg.Edges.Where(sreg.HasNeighbor))
                    {
                        Subregion neighbor = sreg.GetNeighbor(sedge);
                        if (!_outlinedRegions.Contains(neighbor.Region))
                        {
                            objects.PreImageSegments.Add(new SegmentData(sedge.Vertices.ToArray(), Color.Red, 5));
                            objects.Segments.Add(new SegmentData(sedge.Vertices.ToArray(), Color.Black, 3));
                        }
                            
                    }
                }
            }            

            return objects;
        }
    }
}
