using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Topology;
using Utilities;
using WorldSimulation;
using static WorldSimulationForm.Properties.Resources;

namespace WorldSimulationForm
{
    public abstract partial class WorldSimulatorBaseForm
    {
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

        protected void _renderMap(object? sender, EventArgs e)
        {
            if (!_generator.GenerationIsComplete) return;

            OnWorldRendered();

            _image?.Dispose();
            _image = null;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);

            if (_testImage != null)
            {
                e.Graphics.DrawImage(_testImage, _imageRect.Left, _margin);
                _testImage.Dispose();
                _testImage = null;
                return;
            }

            if (_generator == null || !_generator.GenerationIsComplete) return;

            int gridLevel = (int)_gridLevel.Current;

            WorldGrid grid = _generator.GetGrid(gridLevel);

            RenderObjects? objects = null;
            if (_image == null)
            {
                objects = _mapMode.Current switch
                {
                    MapMode.Elevation => _elevationImage(grid),
                    MapMode.Height => _heightImage(grid),
                    MapMode.Temperature => _temperatureImage(),
                    MapMode.Precipitation => _precipitationImage(),
                    MapMode.Biomes => _biomesImage(),
                    MapMode.Pops => _popImage(),
                    MapMode.Cells => _cellsImage(),
                    MapMode.Landmasses => _landmassImage(),
                    _ => throw new Exception()
                };

                if (_multiplier != 0)
                {
                    objects.Multiplier = Math.Pow(2, _multiplier);
                    objects.Origin = _origin;
                }
            }           

            _image = _image ?? HexGridRenderer.Render(grid, _imageRect.Size, objects);
            e.Graphics.DrawImage(_image, _imageRect.Left, _margin);
            objects?.Dispose();

            Bitmap? overlay = null;
            RenderObjects? outline = null;
            if (_newHighlight)
                if (_highlightedRegion != null)
                {
                    outline = _regionOutline(_highlightedRegion);
                    overlay = HexGridRenderer.Render(grid, _image.Size, outline);
                }
                else if (_highlightedArea != null)
                {
                    outline = _areaOutline(_highlightedArea);
                    overlay = HexGridRenderer.Render(grid, _image.Size, outline);
                }
            if (overlay != null)
            {
                e.Graphics.DrawImage(overlay, _imageRect.Left, _margin);
                overlay.Dispose();
            }
            outline?.Dispose();

            _newHighlight = false;
        }

        private RenderObjects _elevationImage(WorldGrid grid)
        {
            Dictionary<Elevation, Brush> brushByElevation = new Dictionary<Elevation, Brush>();
            brushByElevation[Elevation.DeepOcean] = new SolidBrush(Color.MediumBlue);
            brushByElevation[Elevation.ShallowOcean] = new SolidBrush(Color.Blue);
            brushByElevation[Elevation.Lowland] = new SolidBrush(Color.Green);
            brushByElevation[Elevation.Upland] = new SolidBrush(Color.Yellow);
            brushByElevation[Elevation.Highland] = new SolidBrush(Color.Orange);
            brushByElevation[Elevation.Mountain] = new SolidBrush(Color.Brown);

            Pen ridgePen = new Pen(Color.DarkRed, 0);
            ridgePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            ridgePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            RenderObjects objects = new RenderObjects();
            objects.Polygons.AddRange(grid.Cells.Select(c => new PolygonData(c, brushByElevation[_generator.GetElevation(c)])));
            IEnumerable<WorldEdge> edges = _regionBorder.Current ? grid.Edges.Where(_generator.RegionBorder) : grid.Edges.Where(_generator.IsShore);
            Pen blackPen = new Pen(Color.Black);
            objects.Segments.AddRange(edges.Select(e => new SegmentData(e, blackPen)));
            objects.Segments.AddRange(grid.Edges.Where(_generator.HasRidge).Select(e => new SegmentData(e, ridgePen)));

            return objects;
        }

        private RenderObjects _heightImage(WorldGrid grid)
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

            Dictionary<WorldCell, Color> colorByCell = new Dictionary<WorldCell, Color>();
            foreach (WorldCell cell in grid.Cells)
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
                ridgePen.Dispose(); // not used in the subregion path
                return _subregionHeightImageRenderObjects(colorByCell, ridgeColor);
            }
            else
            {
                objects.Polygons.AddRange(grid.Cells.Select(c => new PolygonData(c, colorByCell[c])));
                IEnumerable<WorldEdge> edges = _regionBorder.Current ? grid.Edges.Where(_generator.RegionBorder) : grid.Edges.Where(_generator.IsShore);
                Pen blackPen = new Pen(Color.Black);
                    objects.Segments.AddRange(edges.Select(e => new SegmentData(e, blackPen)));
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
            Dictionary<double, Color> colorByTemperature = _rainbowColors();

            RenderObjects objects = new RenderObjects();

            //bool drawBelts = false; // _chbBelts.Checked; (belt mode unused - per-belt brushes were dead code)

            ColorConverter converter = new ColorConverter();
            Func<Subregion, Brush> colorBySubregion = s =>
            {
                Color color = Interpolation.Interpolate(colorByTemperature, s.Region.Temperature, converter);

                if (_generator.IsSea(s))
                    color = Color.Blue;// Color.FromArgb(75, ...)
                if (_generator.HasRidge(s))
                    color = Color.DarkRed;

                return new SolidBrush(color);
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
                Color color;
                if (drawZones)
                {
                    if (s.Region.Humidity == Humidity.Dry)
                        color = Color.Red;
                    else if (s.Region.Humidity == Humidity.Seasonal)
                        color = Color.Yellow;
                    else
                        color = Color.Green;
                }
                else
                {
                    color = Color.Green; // Interpolation.Interpolate(colorByTemperature, _generator.GetPrecipitation(s), ColorConverter.Converter);
                }

                if (s.Region.IsSea)
                    color = Color.Blue; // Color.FromArgb(75, color);
                if (s.Region.IsRidge)
                    color = Color.DarkRed;

                return new SolidBrush(color);
            };

            objects.Polygons.AddRange(_generator.SubregionGraph.Subregions.Select(s => new PolygonData(s.Vertices, brushBySubregion(s))));

            _addBordersToSubregionImage(objects);

            return objects;
        }

        private RenderObjects _biomesImage()
        {
            RenderObjects objects = new RenderObjects();

            bool imp_textures = _texture.Current.Equals("Texture Imp");
            bool isColor = _texture.Current.Equals("Color");

            Dictionary<Biome, TextureBrush> brushByBiome = new Dictionary<Biome, TextureBrush>();
            if (!isColor)
            {
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
            }

            Dictionary<Biome, Brush> colorByBiome = new Dictionary<Biome, Brush>();
            if (isColor)
            {
                colorByBiome[Biomes.WetTundra] = new SolidBrush(Color.Cyan);
                colorByBiome[Biomes.Tundra] = new SolidBrush(Color.Cyan);
                colorByBiome[Biomes.DryTundra] = new SolidBrush(Color.Cyan);
                colorByBiome[Biomes.WetTaiga] = new SolidBrush(Color.Teal);
                colorByBiome[Biomes.Taiga] = new SolidBrush(Color.Teal);
                colorByBiome[Biomes.DryTaiga] = new SolidBrush(Color.Teal);
                colorByBiome[Biomes.MixedForest] = new SolidBrush(Color.LimeGreen);
                colorByBiome[Biomes.ForestSteppe] = new SolidBrush(Color.LimeGreen);
                colorByBiome[Biomes.TemperateSteppe] = new SolidBrush(Color.Yellow);
                colorByBiome[Biomes.BroadleafForest] = new SolidBrush(Color.LimeGreen);
                colorByBiome[Biomes.SubtropicalSteppe] = new SolidBrush(Color.Yellow);
                colorByBiome[Biomes.SubtropicalDesert] = new SolidBrush(Color.Orange);
                colorByBiome[Biomes.Rainforest] = new SolidBrush(Color.DarkGreen);
                colorByBiome[Biomes.Savanna] = new SolidBrush(Color.Olive);
                colorByBiome[Biomes.TropicalDesert] = new SolidBrush(Color.Orange);
                colorByBiome[Biomes.ShallowOcean] = new SolidBrush(Color.Blue);
                colorByBiome[Biomes.DeepOcean] = new SolidBrush(Color.Blue);
                colorByBiome[Biomes.PolarSea] = new SolidBrush(Color.Blue);
                colorByBiome[Biomes.Mountains] = new SolidBrush(Color.DarkRed);
            }

            foreach (Subregion s in _generator.SubregionGraph.Subregions)
            {
                Biome biome = _generator.RegionMap.GetRegion(s).Biome;

                if (isColor)
                    objects.Polygons.Add(new PolygonData(s.Vertices, colorByBiome[biome]));
                else
                    objects.Polygons.Add(new PolygonData(s.Vertices, brushByBiome[biome]));
            }

            if (_multiplier < 2 && !_texture.Current.Equals("Color"))
            {
                RandomExt rnd = new RandomExt(_seed);

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
                    objects.Disposables.Add(mirror);
                }

                float scale = imp_textures ? 0.75f : 1.0f;

                foreach (Subregion subregion in _generator.SubregionGraph.Subregions.Where(_generator.HasRidge).OrderBy(s => s.Center.Y))
                {
                    Bitmap mountain = rnd.NextItem(mountains);
                    float localScale = (float)(scale * (0.8 + 0.2 * rnd.NextDouble()));
                    objects.Images.Add(new ImageData(subregion.Center, mountain, localScale));
                }
            }

            if (_texture.Current.Equals("Color"))
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
                    Color color = Color.FromArgb(new RandomExt(race.Seed).Next());
                    brushByRace[race] = new SolidBrush(Color.FromArgb(255, color));
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
                        brushes.Add(new SolidBrush(Color.White));
                }
                else
                    brushes.Add(region.IsRidge ? new SolidBrush(Color.Black) : new SolidBrush(Color.Blue));

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
            foreach (Landmass landmass in _generator.LandmassData.Landmasses)
            {
                int[] vector = new RandomExt(landmass.Seed).NextVector(3, 256);
                brushByLandmass[landmass] = new SolidBrush(Color.FromArgb(vector[0], vector[1], vector[2]));
            }

            Func<Subregion, Brush> getBrush = subregion =>
            {
                WorldSimulation.Region region = subregion.Region;
                if (region.IsSea)
                    return new SolidBrush(Color.Blue);
                else if (region.Biome == Biomes.Mountains)
                    return new SolidBrush(Color.Black);
                else
                    return brushByLandmass[region.Landmass];
            };

            objects.Polygons.AddRange(_generator.SubregionGraph.Subregions.Select(s => new PolygonData(s.Vertices, getBrush(s))));
            Brush landmassMark = new SolidBrush(Color.Black);
            objects.Vertices.AddRange(_generator.LandmassData.Landmasses.Select(s => new VertexData(s.Center, landmassMark)));

            Pen blackPen = new Pen(Color.Black);

            foreach (Landmass landmass in _generator.LandmassData.Landmasses)
            {
                foreach (Landmass neighbor in landmass.Neighbors)
                {
                    double x = neighbor.Center.X;
                    if (x - landmass.Center.X > _generator.SubregionGraph.Width / 2)
                        x -= _generator.SubregionGraph.Width;
                    else if (x - landmass.Center.X < -_generator.SubregionGraph.Width / 2)
                        x += _generator.SubregionGraph.Width;

                    objects.Segments.Add(new SegmentData([landmass.Center, new(x, neighbor.Center.Y)], blackPen));
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

            List<SolidBrush> brushes = ((KnownColor[])Enum.GetValues(typeof(KnownColor))).Where(kc => !kc.Equals(KnownColor.Transparent)).
                Select(kc => new SolidBrush(Color.FromKnownColor(kc))).ToList();

            Dictionary<WorldSimulation.Region, SolidBrush> bbr = _generator.RegionMap.Regions.ToDictionary(r => r, r => random.NextItem(brushes));
            ConcurrentDictionary<WorldSimulation.Region, SolidBrush> brushByRegion = new ConcurrentDictionary<WorldSimulation.Region, SolidBrush>(bbr);

            int horRes = 1600;
            double cellSize = graph.Width / horRes;
            int verRes = (int)(graph.Height / cellSize);

            Stopwatch sw = Stopwatch.StartNew();

            Action<int> doColumn = i =>
            {
                for (int j = 0; j < verRes; j++)
                {
                    Vector2[] v =
                    [
                        new Vector2(cellSize * i, cellSize * j),
                        new Vector2(cellSize * (i + 1), cellSize * j),
                        new Vector2(cellSize * (i + 1), cellSize * (j + 1)),
                        new Vector2(cellSize * i, cellSize * (j + 1)),
                    ];
                    Vector2 c = new Vector2(cellSize * (i + 0.5), cellSize * (j + 0.5));
                    Subregion? subregion = graph.SpatialIndex.FindPolygonContainingPoint(c.X, c.Y);

                    if (subregion == null)
                        subregion = graph.SpatialIndex.FindPolygonContainingPoint(c.X + graph.Width, c.Y);

                    if (subregion == null)
                        subregion = graph.SpatialIndex.FindPolygonContainingPoint(c.X - graph.Width, c.Y);

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

            ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = 4 };
            Parallel.For(0, horRes, options, doColumn);

            Debug.WriteLine($"Cell map of size {horRes} x {verRes} built in {sw.ElapsedMilliseconds} ms");

            return objects;
        }

        private RenderObjects _subregionHeightImageRenderObjects(Dictionary<WorldCell, Color> colorByCell, Color ridgeColor)
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

        void _addBordersToSubregionImage(RenderObjects objects)
        {
            Color subregionBorderColor = Color.FromArgb(20, Color.Black);
            Pen subregionPen = new Pen(subregionBorderColor);
            Brush subregionBrush = new SolidBrush(subregionBorderColor);
            Pen blackPen = new Pen(Color.Black);

            foreach (Subregion sreg in _generator.SubregionGraph.Subregions)
            {
                foreach (SubregionEdge sedge in sreg.Edges.Where(sreg.HasNeighbor))
                {
                    Subregion neighbor = sreg.GetNeighbor(sedge);
                    if (_regionBorder && !sreg.SameRegion(neighbor))
                        objects.PreImageSegments.Add(new SegmentData(sedge.Vertices, blackPen));
                    else if (_generator.IsLand(sreg) && _generator.IsSea(neighbor))
                        objects.PreImageSegments.Add(new SegmentData(sedge.Vertices, blackPen));
                    else if (_subregionBorder)
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

                if (_subregionBorder && _multiplier > 1)
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

        private RenderObjects _areaOutline(IEnumerable<WorldSimulation.Region> regions)
        {
            RenderObjects objects = new RenderObjects();
            HashSet<WorldSimulation.Region> _outlinedRegions = [.. regions];

            if (_multiplier != 0)
            {
                objects.Multiplier = Math.Pow(2, _multiplier);
                objects.Origin = _origin;
            }

            foreach (WorldSimulation.Region region in regions)
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