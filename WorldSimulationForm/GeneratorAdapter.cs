using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldSimulation;
using WorldSimulation.HistorySimulation;

namespace WorldSimulationForm
{
    // used to handle both new and old generators
    public interface IGenerator
    {
        event EventHandler<string> LogUpdated;
        event EventHandler OnGenerationComplete;
        int GridLevels { get; }
        GenerationParameters Parameters { get; }
        RegionMap RegionMap { get; }
        HistorySimulator History { get; }
        SubregionGraph SubregionGraph { get; }
        bool GenerationIsComplete { get; }
        LandmassData LandmassData { get; }

        void Generate();
        void Regenerate();
        void Regenerate(int seed);
        WorldGrid GetGrid(int gridLevel);
        bool RegionBorder(WorldEdge edge);
        bool IsShore(WorldEdge edge);
        bool HasRidge(WorldEdge edge);
        bool HasRidge(Subregion subregion);

        int GetCountByElevation(Elevation e);
        bool IsSea(WorldCell cell);
        bool IsSea(Subregion subregion);
        int GetHeight(WorldCell cell);
        bool LastGrid(WorldGrid grid);
        bool IsLand(WorldCell cell);
        bool IsLand(Subregion subregion);
        bool HasRiver(WorldCell cell);
        bool HasRiver(WorldEdge edge);

        Elevation GetElevation(WorldCell cell);
        WorldCell GetDrainage(WorldCell c);

        WorldCell GetDrainage(WorldEdge e);
    }

    // adapter of old world generator
    class LegacyGeneratorAdapter : IGenerator
    {
        WorldGeneratorLegacy _gen;
        public LegacyGeneratorAdapter(WorldGeneratorLegacy generator)
        {
            _gen = generator;
            _gen.LogUpdated += LogUpdated.Invoke;
            _gen.OnGenerationComplete += OnGenerationComplete.Invoke;
        }

        public int GridLevels => _gen.GridLevels;

        public GenerationParameters Parameters => _gen.Parameters;

        public RegionMap RegionMap =>_gen.RegionMap;

        public HistorySimulator History => _gen.History;

        public SubregionGraph SubregionGraph => _gen.SubregionGraph;

        public bool GenerationIsComplete => _gen.GenerationIsComplete;

        public LandmassData LandmassData => _gen.LandmassData;

        public event EventHandler<string> LogUpdated = delegate { };
        public event EventHandler OnGenerationComplete = delegate { };

        public void Generate() => _gen.Generate();

        public int GetCountByElevation(Elevation e) => _gen.GetCountByElevation(e);
        public WorldCell GetDrainage(WorldCell c) => _gen.GetDrainage(c);
        public WorldCell GetDrainage(WorldEdge e) => _gen.GetDrainage(e);
        public Elevation GetElevation(WorldCell cell) => _gen.GetElevation(cell);
        public WorldGrid GetGrid(int gridLevel) => _gen.GetGrid(gridLevel);
        public int GetHeight(WorldCell cell) => _gen.GetHeight(cell);
        public bool HasRidge(WorldEdge edge) => _gen.HasRidge(edge);
        public bool HasRidge(Subregion subregion) => _gen.HasRidge(subregion);
        public bool HasRiver(WorldCell cell) => _gen.HasRiver(cell);
        public bool HasRiver(WorldEdge edge) => _gen.HasRiver(edge);
        public bool IsLand(WorldCell cell) => _gen.IsLand(cell);
        public bool IsLand(Subregion subregion) => _gen.IsLand(subregion);
        public bool IsSea(WorldCell cell) => _gen.IsSea(cell);
        public bool IsSea(Subregion subregion) => _gen.IsSea(subregion);
        public bool IsShore(WorldEdge edge) => _gen.IsShore(edge);
        public bool LastGrid(WorldGrid grid) => _gen.LastGrid(grid);
        public void Regenerate() => _gen.Regenerate();
        public void Regenerate(int seed) => _gen.Regenerate(seed);
        public bool RegionBorder(WorldEdge edge) => _gen.RegionBorder(edge);
    }

    // adapter of new world generator
    class GeneratorAdapter : IGenerator
    {
        WorldGenerator _gen;

        public GeneratorAdapter(WorldGenerator generator)
        {
            _gen = generator;
        }

        public int GridLevels => _gen.GridLevels;

        public GenerationParameters Parameters => throw new NotImplementedException();

        public RegionMap RegionMap => throw new NotImplementedException();

        public HistorySimulator History => throw new NotImplementedException();

        public SubregionGraph SubregionGraph => throw new NotImplementedException();

        public bool GenerationIsComplete => throw new NotImplementedException();

        public LandmassData LandmassData => throw new NotImplementedException();

        public event EventHandler<string> LogUpdated = delegate { };
        public event EventHandler OnGenerationComplete = delegate { };

        public void Generate() => _gen.Generate();

        public int GetCountByElevation(Elevation e)
        {
            throw new NotImplementedException();
        }

        public WorldCell GetDrainage(WorldCell c)
        {
            throw new NotImplementedException();
        }

        public WorldCell GetDrainage(WorldEdge e)
        {
            throw new NotImplementedException();
        }

        public Elevation GetElevation(WorldCell cell)
        {
            throw new NotImplementedException();
        }

        public WorldGrid GetGrid(int gridLevel)
        {
            throw new NotImplementedException();
        }

        public int GetHeight(WorldCell cell)
        {
            throw new NotImplementedException();
        }

        public bool HasRidge(WorldEdge edge)
        {
            throw new NotImplementedException();
        }

        public bool HasRidge(Subregion subregion)
        {
            throw new NotImplementedException();
        }

        public bool HasRiver(WorldCell cell)
        {
            throw new NotImplementedException();
        }

        public bool HasRiver(WorldEdge edge)
        {
            throw new NotImplementedException();
        }

        public bool IsLand(WorldCell cell)
        {
            throw new NotImplementedException();
        }

        public bool IsLand(Subregion subregion)
        {
            throw new NotImplementedException();
        }

        public bool IsSea(WorldCell cell)
        {
            throw new NotImplementedException();
        }

        public bool IsSea(Subregion subregion)
        {
            throw new NotImplementedException();
        }

        public bool IsShore(WorldEdge edge)
        {
            throw new NotImplementedException();
        }

        public bool LastGrid(WorldGrid grid)
        {
            throw new NotImplementedException();
        }

        public void Regenerate() => _gen.Regenerate();

        public void Regenerate(int seed)
        {
            throw new NotImplementedException();
        }

        public bool RegionBorder(WorldEdge edge)
        {
            throw new NotImplementedException();
        }
    }
}
