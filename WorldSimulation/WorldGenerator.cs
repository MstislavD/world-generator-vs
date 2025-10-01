using HexGrid;
using RandomExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parameters;
using PointLocation;

namespace WorldSimulation
{
    public enum Elevation { DeepOcean, ShallowOcean, Lowland, Upland, Highland, Mountain}
    public class WorldGenerator
    {
        int _gridWidth = 10; // 10;
        int _gridHeight = 7; // 7;
        double _lowlandPct = 0.6;
        double _uplandPct = 0.3;

        RandomExt random;

        GenerationParameters _parameters;

        List<Grid> _grids;

        public WorldGenerator(IParameterSupplier supplier)
        {
            _parameters = new GenerationParameters(supplier);
        }

        public WorldGenerator() { }

        public void Regenerate()
        {
            _parameters.RegenerateSeeds();
            Generate();
        }

        public void Regenerate(int seed)
        {
            _parameters.RegenerateSeeds(seed);
            Generate();
        }

        public void Generate(GenerationParameters parameters)
        {
            _parameters = parameters;
            Generate();
        }

        public void Generate()
        {
            random = new RandomExt(_parameters.MainSeed);
            RandomExt subregionRandom = new RandomExt(_parameters.SubregionSeed);
            RandomExt heightRandom = new RandomExt(_parameters.HeightSeed);
            NamingLanguage = new Language(Seed);

            _grids = new List<Grid>();
            CellData = new Dictionary<HexCell, CellData>();
            EdgeData = new Dictionary<Edge, EdgeData>();

            Grid grid = new Grid(_gridWidth, _gridHeight);
            _grids.Add(grid);
            _addData(grid);

            if (_parameters.MapScript.Current.StringValue == "Random")
                ElevationGenerator.GenerateRandom(this, grid, random);
            else if (_parameters.MapScript.Current.StringValue == "One Continent")
                ElevationGenerator.GenerateScriptPangea(this, grid, random);
            else if (_parameters.MapScript.Current.StringValue == "Two Continents")
                ElevationGenerator.GenerateScriptTwoContinents(this, grid, random);
            else if (_parameters.MapScript.Current.StringValue == "Three Continents")
                ElevationGenerator.GenerateScriptThreeContinents(this, grid, random);

            for (int i = 0; i < GridLevels; i++)
            {
                bool expandSmoothely = i == GridLevels - 1 && _parameters.UniformRegionSize;
                RandomExt rndLastGrid = i == GridLevels - 1 ? subregionRandom : random;
                ExpandedHexGrid expandedGrid = expandSmoothely ? HexGridExpander.Expand(grid, rndLastGrid, 0) : HexGridExpander.Expand(grid, rndLastGrid);
                grid = expandedGrid.Grid;
                _grids.Add(grid);
                _addData(grid);

                ElevationGenerator.GenerateFromParent(this, expandedGrid);

                if (i < GridLevels - 1)
                {
                    ElevationGenerator.GenerateModify(this, grid, random);
                }

                if (i == GridLevels - 2)
                {
                    HeightGenerator.Generate(this, grid, heightRandom);
                }
            }

            if (_parameters.RegionSmoothing)
                RegionSmoother.Smooth(this, grid);

            SubregionGraph = new SubregionGraph(grid, this);
            RegionMap = new RegionMap(this);

            if (_parameters.DeformationDetails)
            {
                double cellSize = SubregionGraph.Width / _parameters.DeformationFrequencyMax.Current.IntValue;
                double targetEdgeLength = cellSize / _parameters.DetalizationFactor;
                EdgeDetailer.Detail(SubregionGraph, targetEdgeLength);
            }

            if (_parameters.RegionDeformation)
            {
                Random rndDeformation = new RandomExt(_parameters.DeformationSeed);

                double power = 1.0 / (_parameters.DeformationFrequencies - 1);
                int frequency = _parameters.DeformationFrequencyMin.Current.IntValue;
                double strength = _parameters.DeformationStrengthMax.Current.DoubleValue;
                double frequencyMultiplier = Math.Pow(_parameters.DeformationFrequencyMax.Current.IntValue / frequency, power);
                double strengthMultiplier = Math.Pow(_parameters.DeformationStrengthMin.Current.DoubleValue / strength, power);

                for (int i = 0; i < _parameters.DeformationFrequencies; i++)
                {
                    PerlinDeformer.Deform(SubregionGraph, frequency, strength, rndDeformation);
                    frequency = (int)(frequency * frequencyMultiplier);
                    strength *= strengthMultiplier;
                }
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            SubregionGraph.GenerateLocator(heightRandom);
            Console.WriteLine($"Subregion point locator structure calculated in :{sw.ElapsedMilliseconds} ms");


            HeightGenerator.Generate(this);
            TemperatureGenerator.Generate(this, random);
            PrecipitationGenerator.Generate(this);
            RiverGenerator.GenerateForRegions(this, heightRandom);
            RiverGenerator.GenerateForSubregions(this, heightRandom);

            LandmassData = new LandmassData(this, random);

            History = new HistorySimulation.HistorySimulator(Seed, this);
            History.LogUpdate += (s, entry) => LogUpdated?.Invoke(s, entry);
            History.Simulate();

            GenerationIsComplete = true;
            GenerationComplete?.Invoke(this, null);
        }

        public event EventHandler GenerationComplete;
        public event EventHandler<string> LogUpdated;
        public Grid GetGrid(int level) => _grids[level];
        public int GridLevels { get; } = 3;
        public SubregionGraph SubregionGraph { get; private set; }
        public RegionMap RegionMap { get; private set; }
        public LandmassData LandmassData { get; private set; }
        public IEnumerable<HexCell> RegionCells => _grids[GridLevels - 1].Cells;
        public GenerationParameters Parameters => _parameters;
        public CellData GetData(HexCell cell) => CellData[cell];
        public Elevation GetElevation(HexCell cell) => CellData[cell].Elevation;
        public HexCell GetCellParent(HexCell cell) => CellData[cell].Parent ?? cell;
        public HexCell GetDrainage(HexCell cell) => RegionMap.GetRegion(cell).Drainage.Cell ?? cell;
        public HexCell GetDrainage(Edge edge) => RegionMap.GetRegion(edge).Drainage.Cell;
        public Edge GetEdgeParent(Edge edge) => EdgeData[edge].Parent ?? edge;
        public bool RegionBorder(Edge edge) =>
            CellData[edge.Cell1].Parent == null || (edge.Cell2 != null && !CellData[edge.Cell1].Parent.Equals(CellData[edge.Cell2].Parent));
        public bool IsSea(HexCell cell) => CellData[cell].Elevation < Elevation.Lowland;
        public bool IsSea(Subregion sregion) => sregion.Type != SubregionType.Edge && IsSea(sregion.ParentCell);
        public bool IsLand(HexCell cell) => CellData[cell].Elevation > Elevation.ShallowOcean;
        public bool IsLand(Subregion sregion) => sregion.Type == SubregionType.Edge || IsLand(sregion.ParentCell);
        public bool PossibleRidge(Edge edge) => edge.Cell2 != null && (IsLand(edge.Cell1) || IsLand(edge.Cell2)) && !EdgeData[edge].Ridge;
        public bool HasRidge(Edge edge) => edge.Cell2 != null && (IsLand(edge.Cell1) || IsLand(edge.Cell2)) && EdgeData[edge].Ridge;
        public bool HasRidge(Subregion sRegion) => sRegion.Type == SubregionType.Edge && HasRidge(sRegion.ParentEdge);
        public bool HasRiver(HexCell cell) => RegionMap.GetRegion(cell).River;
        public bool HasRiver(Edge edge) => RegionMap.GetRegion(edge).River;
        public bool NearSea(HexCell cell) => cell.Neighbors.Any(IsSea);
        public bool NearLand(HexCell cell) => cell.Neighbors.Any(IsLand);
        public bool IsShore(Edge edge) => edge.Cells.Count() == 2 && IsLand(edge.Cell1) != IsLand(edge.Cell2);
        public int GetHeight(HexCell cell) => CellData[cell].Height;
        public int GetHeight(Subregion subregion) => subregion.Type == SubregionType.Cell ? CellData[subregion.ParentCell].Height : 1;
        public Humidity GetHumidity(Subregion subregion) => subregion.Region.Humidity;
        public Belt GetBelt(Subregion subregion) => subregion.Region.Belt;
        public double GetHeightD(Subregion subregion) => GetHeight(subregion);
        public bool LastGrid(Grid grid) => grid.Equals(_grids[GridLevels]);
        internal Dictionary<HexCell, CellData> CellData { get; private set; }
        internal Dictionary<Edge, EdgeData> EdgeData { get; private set; }
        public bool GenerationIsComplete { get; private set; }
        public int Seed => _parameters.MainSeed;
        public Language NamingLanguage { get; private set; }
        public HistorySimulation.HistorySimulator History { get; private set; }

        public double SeaPct
        {
            get
            {
                string landsize = _parameters.LandSize.Current.StringValue;
                if (landsize == "Tiny")
                    return 0.9;
                else if (landsize == "Small")
                    return 0.8;
                else if (landsize == "Medium")
                    return 0.7;
                else if (landsize == "Large")
                    return 0.6;
                else if (landsize == "Huge")
                    return 0.5;
                else if (landsize == "Colossal")
                    return 0.4;
                else
                    return 0.1;
            }
        }

        public Vertex Center(Region region)
        {
            double width = SubregionGraph.Width;
            double halfWidth = width / 2;
            Vertex center = new Vertex(0, 0);
            Vertex worldShift = width * new Vertex(1, 0);
            int totalSize = 0;

            foreach (Subregion subregion in region.Subregions)
            {
                Vertex subregionCenter = new Vertex(subregion.Center);

                if (totalSize > 0)
                {
                    double deltaX = subregionCenter.X - center.X;
                    if (deltaX > halfWidth)
                        subregionCenter -= worldShift;
                    else if (deltaX < -halfWidth)
                        subregionCenter += worldShift;
                }

                center = totalSize * center + subregionCenter;
                totalSize = totalSize + 1;
                center = 1.0 / totalSize * center;

                if (center.X > width)
                    center -= worldShift;
                else if (center.X < 0)
                    center += worldShift;
            }

            return center;
        }

        int _regionCount => RegionMap.CountNonRidge;

        public int GetCountByElevation(Elevation elevation)
        {
            int landCellsCount = (int)(_regionCount * (1 - SeaPct));
            if (elevation == Elevation.Lowland)
            {
                return (int)(landCellsCount * _lowlandPct);
            }
            else if (elevation == Elevation.Upland)
            {
                return (int)(landCellsCount * _uplandPct);
            }
            else if (elevation == Elevation.Highland)
            {
                return (int)(landCellsCount * (1 - _lowlandPct - _uplandPct));
            }
            else if (elevation == Elevation.ShallowOcean)
            {
                return (int)(_regionCount * SeaPct * (1 - _parameters.DeepPct.Current.DoubleValue));
            }
            else if (elevation == Elevation.DeepOcean)
            {
                return (int)(_regionCount * SeaPct * _parameters.DeepPct.Current.DoubleValue);
            }
            else
            {
                return 0;
            }
        }

        public bool IsSeedParameter(Parameter parameter) => Parameters.IsSeed(parameter);

        void _addData(Grid grid)
        {
            foreach (HexCell cell in grid.Cells)
            {
                CellData[cell] = new CellData();
            }
            foreach (Edge edge in grid.Edges)
            {
                EdgeData[edge] = new EdgeData();
            }
        }
    }
}
