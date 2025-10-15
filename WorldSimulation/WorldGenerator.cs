using Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using TrapezoidSpatialIndex;
using System.Diagnostics;

namespace WorldSimulation
{
    public class WorldCell : LayerHexCell<WorldCell, WorldEdge> { }
    public class WorldEdge : Edge<WorldCell> { }
    public class WorldGrid : HexGrid<WorldCell, WorldEdge>
    {
        public WorldGrid(int columns, int rows) : base(columns, rows) { }
    }
    public interface IGeneratorCell<TCell>
    {
        public double SeaPct { get; }
        public Elevation GetElevation(TCell cell);
        public int GetHeight(TCell cell);
        public void SetElevation(TCell cell, Elevation elevation);
        public void SetHeight(TCell cell, int height);
        public void SetParent(TCell cell, TCell parent);
        public void IncreaseElevation(TCell cell);
        public bool IsSea(TCell cell);
        public bool IsLand(TCell cell);
        public TCell GetCellParent(TCell cell);
    }
    public interface IGeneratorEdge<TEdge>
    {
        public bool GetRidge(TEdge edge);
        public void SetParent(TEdge edge, TEdge parent);
        public void SetRidge(TEdge edge, bool r);
        public bool PossibleRidge(TEdge e);
    }

    public interface IGenerator
    {
        public GenerationParameters Parameters { get; }
    }
    public enum Elevation { DeepOcean, ShallowOcean, Lowland, Upland, Highland, Mountain }

    public class WorldGenerator : IGenerator, IGeneratorCell<HexCell>, IGeneratorEdge<Edge>
    {
        int _gridWidth = 10; // 10;
        int _gridHeight = 7; // 7;
        double _lowlandPct = 0.6;
        double _uplandPct = 0.3;

        RandomExt random;

        GenerationParameters _parameters = new GenerationParameters();

        List<HexGrid> _grids;

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

        public void Generate()
        {
            random = new RandomExt(_parameters.MainSeed);
            RandomExt subregionRandom = new RandomExt(_parameters.SubregionSeed);
            RandomExt heightRandom = new RandomExt(_parameters.HeightSeed);
            NamingLanguage = new Language(Seed);

            _grids = new List<HexGrid>();
            CellData = new Dictionary<HexCell, CellData>();
            EdgeData = new Dictionary<Edge, EdgeData>();

            HexGrid grid = new HexGrid(_gridWidth, _gridHeight);
            _grids.Add(grid);
            _addData(grid);

            Action<WorldGenerator, HexGrid, RandomExt> generateContinents = _parameters.MapScript.Current switch
            {
                MapScript.Random => ElevationGenerator.GenerateRandom<WorldGenerator, HexGrid, HexCell, Edge>,
                MapScript.One_continent => ElevationGenerator.GenerateScriptPangea,
                MapScript.Two_continents => ElevationGenerator.GenerateScriptTwoContinents,
                MapScript.Three_continents => ElevationGenerator.GenerateScriptThreeContinents,
                _ => throw new Exception()
            };
            generateContinents(this, grid, random);

            for (int i = 0; i < GridLevels; i++)
            {
                bool expandSmoothely = i == GridLevels - 1 && _parameters.UniformRegionSize;
                RandomExt rndLastGrid = i == GridLevels - 1 ? subregionRandom : random;
                ExpandedHexGrid expandedGrid = expandSmoothely ? HexGridExpander.Expand(grid, rndLastGrid, 0) : HexGridExpander.Expand(grid, rndLastGrid);
                grid = expandedGrid.Grid;
                _grids.Add(grid);
                _addData(grid);

                ElevationGenerator.GenerateFromParent<WorldGenerator, HexGrid, HexCell, Edge>(this, expandedGrid);

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
                double cellSize = SubregionGraph.Width / _parameters.DeformationFrequencyMax.Current;
                double targetEdgeLength = cellSize / _parameters.DetalizationFactor;
                EdgeDetailer.Detail(SubregionGraph, targetEdgeLength);
            }

            if (_parameters.RegionDeformation)
            {
                Random rndDeformation = new RandomExt(_parameters.DeformationSeed);

                double power = 1.0 / (_parameters.DeformationFrequencies - 1);
                int frequency = _parameters.DeformationFrequencyMin;
                double strength = _parameters.DeformationStrengthMax;
                double frequencyMultiplier = Math.Pow(_parameters.DeformationFrequencyMax / frequency, power);
                double strengthMultiplier = Math.Pow(_parameters.DeformationStrengthMin / strength, power);

                for (int i = 0; i < _parameters.DeformationFrequencies; i++)
                {
                    PerlinDeformer.Deform(SubregionGraph, frequency, strength, rndDeformation);
                    frequency = (int)(frequency * frequencyMultiplier);
                    strength *= strengthMultiplier;
                }
            }

            Stopwatch sw = Stopwatch.StartNew();
            SubregionGraph.GenerateSpatialIndex(heightRandom);
            Debug.WriteLine($"Subregion point locator structure calculated in :{sw.ElapsedMilliseconds} ms");


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
            OnGenerationComplete?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler OnGenerationComplete;
        public event EventHandler<string> LogUpdated;

        public HexGrid GetGrid(int level) => _grids[level];
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
        public bool LastGrid(HexGrid grid) => grid.Equals(_grids[GridLevels]);
        internal Dictionary<HexCell, CellData> CellData { get; private set; }
        internal Dictionary<Edge, EdgeData> EdgeData { get; private set; }
        public bool GenerationIsComplete { get; private set; }
        public int Seed => _parameters.MainSeed;
        public Language NamingLanguage { get; private set; }
        public HistorySimulation.HistorySimulator History { get; private set; }

        public double SeaPct => (LandSize)_parameters.LandSize switch
        {
            LandSize.Tiny => 0.9,
            LandSize.Small => 0.8,
            LandSize.Medium => 0.7,
            LandSize.Large => 0.6,
            LandSize.Huge => 0.5,
            LandSize.Colossal => 0.4,
            _ => 0.1
        };

        public Vector2 Center(Region region)
        {
            double width = SubregionGraph.Width;
            double halfWidth = width / 2;
            Vector2 center = new Vector2(0, 0);
            Vector2 worldShift = width * new Vector2(1, 0);
            int totalSize = 0;

            foreach (Subregion subregion in region.Subregions)
            {
                Vector2 subregionCenter = new Vector2(subregion.Center);

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
                return (int)(_regionCount * SeaPct * (1 - _parameters.DeepPct));
            }
            else if (elevation == Elevation.DeepOcean)
            {
                return (int)(_regionCount * SeaPct * _parameters.DeepPct);
            }
            else
            {
                return 0;
            }
        }

        void _addData(HexGrid grid)
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

        public void SetElevation(HexCell cell, Elevation elevation) => CellData[cell].Elevation = elevation;

        public void IncreaseElevation(HexCell cell) => CellData[cell].Elevation += 1;

        public void SetRidge(Edge edge, bool r) => EdgeData[edge].Ridge = r;

        public void SetParent(HexCell cell, HexCell parent) => CellData[cell].Parent = parent;

        public void SetHeight(HexCell cell, int height) => CellData[cell].Height = height;

        public void SetParent(Edge edge, Edge parent) => EdgeData[edge].Parent = parent;

        public bool GetRidge(Edge edge) => EdgeData[edge].Ridge;
    }
}
