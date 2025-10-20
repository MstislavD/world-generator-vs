using Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldSimulation.HistorySimulation;

namespace WorldSimulation
{
    public class Region : INode<Region>
    {
        List<Subregion> _subregions;
        List<RegionTrait> _traits;
        List<Population> _pops;
        RegionMap _map;

        public string Name { get; internal set; }
        public double Height { get; internal set; }
        public double Temperature { get; internal set; }
        public Belt Belt { get; internal set; }
        public Humidity Humidity { get; internal set; }
        public Biome Biome { get; internal set; }
        public HexCell Cell { get; }
        public Edge Edge { get; }
        public double Water { get; internal set; }
        public Region Drainage { get; internal set; }
        public bool River { get; internal set; }
        public Landmass Landmass { get; internal set; }
        public bool IsSea => Height < 0;
        public bool IsLand => !IsSea;
        public bool IsRidge => Edge != null;
        public bool IsFlat => !IsSea && !IsRidge;
        public IEnumerable<Subregion> Subregions => _subregions;
        public IEnumerable<RegionTrait> Traits => _traits;
        public IEnumerable<Population> Pops => _pops;
        public int PopCount() => _pops.Count;
        public int PopCount(Race race) => _pops.Count(p => p.Race == race);
        public int Size => _subregions.Count;
        public Vector2 Center
        {
            get
            {
                double width = _map.Generator.SubregionGraph.Width;
                double halfWidth = width / 2;
                Vector2 center = new Vector2(0, 0);
                Vector2 worldShift = width * new Vector2(1, 0);
                int totalSize = 0;

                foreach (Subregion subregion in _subregions)
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

                return new Vector2(Math.Round(center.X, 1), Math.Round(center.Y, 1));

                //return new Vertex(Math.Round(_subregions.Average(s => s.Center.X), 1), Math.Round(_subregions.Average(s => s.Center.Y), 1));
            }
        }

        public override string ToString() => $"{Name} ({Center})";
        public Region(RegionMap map, HexCell cell) : this(map)
        {
            Cell = cell;
        }
        public Region(RegionMap map, Edge edge) : this(map)
        {
            Edge = edge;
        }
        Region(RegionMap map)
        {
            _map = map;
            _subregions = new List<Subregion>();
            _traits = new List<RegionTrait>();
            _pops = new List<Population>();
        }
        public void AddSubregion(Subregion subregion)
        {
            _subregions.Add(subregion);
            subregion.Region = this;
        }
        public void AddTrait(RegionTrait trait)
        {
            _traits.Add(trait);
        }
        public void AddPop (Population pop)
        {
            _pops.Add(pop);
        }
        public void RemovePop(Population pop)
        {
            if (!_pops.Remove(pop))
                throw new Exception("The population to be removed is not present");           
        }
        public IEnumerable<Region> CellNeighbors
        {
            get
            {
                if (Cell != null)
                {
                    return Cell.Neighbors.Select(_map.GetRegion).Where(r => HasBorder(r, this));
                }
                else
                {
                    return Edge.Cells.Select(_map.GetRegion);
                }
            }
        }

        public IEnumerable<Region> AllNeighbors
        {
            get
            {
                if (Cell != null)
                {
                    foreach (HexCell cellNeighbor in Cell.Neighbors)
                    {
                        Region flat = _map.GetRegion(cellNeighbor);
                        yield return HasBorder(flat, this) ? flat : _map.GetRegion(Cell.GetEdgeByNeighbor(cellNeighbor));
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public IEnumerable<Region> FlatNeighbors => CellNeighbors.Where(n => n.IsFlat);

        public IEnumerable<Region> Neighbors => CellNeighbors;

        public bool HasBorder(Region region1, Region region2) => !_map.ContainsEdge(region1.Cell.GetEdgeByNeighbor(region2.Cell));
    }
}
