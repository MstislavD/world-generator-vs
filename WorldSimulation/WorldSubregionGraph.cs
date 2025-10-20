using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using Utilities;
using TrapezoidSpatialIndex;
using System.Diagnostics;

namespace WorldSimulation
{
    public class WorldSubregion : Subregion<WorldSubregion, HexCell, Edge> { }

    public class WorldSubregionGraph : SubregionGraph<WorldGenerator, WorldSubregion, HexGrid, HexCell, Edge>
    {
        public WorldSubregionGraph(HexGrid grid, WorldGenerator generator) : base(grid, generator) { }
    }

    public enum SubregionType { Cell, Edge }

    public class SubregionGraph<TGen, TSubregion, TGrid, TCell, TEdge>
        where TGen : IGeneratorCell<TCell>, IGeneratorEdge<TEdge>
        where TSubregion : Subregion<TSubregion, TCell , TEdge>, new()
        where TGrid : IHexGrid, IGrid<TCell>, IEdges<TEdge>
        where TCell : HexCell<TCell, TEdge>
        where TEdge : LineSegment, IEdge<TCell>, new()
    {
        List<TSubregion> _subregions = new List<TSubregion>();
        Dictionary<TEdge, TSubregion> _subregionByEdge = new Dictionary<TEdge, TSubregion>();
        Dictionary<TCell, TSubregion> _subregionByCell = new Dictionary<TCell, TSubregion>();
        Dictionary<TCell, List<TSubregion>> _subregionsByCellRegion = new Dictionary<TCell, List<TSubregion>>();
        Dictionary<TEdge, List<TSubregion>> _subregionsByEdgeRegion = new Dictionary<TEdge, List<TSubregion>>();

        double _delta = 0.5;
        const bool _useRidgeSmoothing = true;

        public SubregionGraph(TGrid grid, TGen generator)
        {
            Width = grid.Width;
            Height = grid.Height;

            foreach (TEdge edge in grid.Edges.Where(generator.HasRidge))
            {
               _createEdgeSubregion(edge, _subregionByEdge, generator);
            }

            foreach (TCell cell in grid.Cells)
            {
                _createCellSubregion(cell, _subregionByCell, generator);
            }

            foreach (TSubregion subregion in _subregions.Where(sr => sr.Type == SubregionType.Edge))
            {
                _calculateGeometryRidge(subregion, generator);

                if (!_subregionsByEdgeRegion.ContainsKey(subregion.ParentEdge))
                {
                    _subregionsByEdgeRegion[subregion.ParentEdge] = new List<TSubregion>();
                }
                _subregionsByEdgeRegion[subregion.ParentEdge].Add(subregion);
            }

            foreach (TSubregion subregion in _subregions.Where(sr => sr.Type == SubregionType.Cell))
            {
                _calculateGeometryCell(subregion, generator);

                if (!_subregionsByCellRegion.ContainsKey(subregion.ParentCell))
                {
                    _subregionsByCellRegion[subregion.ParentCell] = new List<TSubregion>();
                }
                _subregionsByCellRegion[subregion.ParentCell].Add(subregion);
            }
        }

        public IEnumerable<TSubregion> Subregions => _subregions;
        public double Width { get; }
        public double Height { get; }
        public IEnumerable<TSubregion> CellSubregions => _subregions.Where(sr => sr.Type == SubregionType.Cell);
        public IEnumerable<TSubregion> EdgeSubregions => _subregions.Where(sr => sr.Type == SubregionType.Edge);
        public TSubregion GetSubregion(TCell cell) => _subregionByCell[cell];
        public ISpatialIndex<TSubregion>? SpatialIndex { get; private set; }
        public IEnumerable<TSubregion> GetSubregions(TCell region) => _subregionsByCellRegion[region];
        public IEnumerable<TSubregion> GetSubregions(TEdge edge) => _subregionsByEdgeRegion[edge];

        // This method assumes concrete implementation of HexCell
        private void _calculateGeometryCell(TSubregion subregion, TGen generator)
        {
            TCell cell = subregion.Cell;
            List<Vector2> subregionVertices = new List<Vector2>();

            for (int direction = 0; direction < 7; direction++)
            {
                Vector2[] vertices = _cellRegionVertices(generator, cell, direction);
                subregionVertices.AddRange(vertices);
            }

            for (int direction = 0; direction < 6; direction++)
            {
                SubregionEdge sedge = new SubregionEdge();
                TCell neighborCell = cell.GetNeighbor(direction);
                TEdge neighborEdge = cell.GetEdge(direction);
                subregion.AddEdge(sedge);

                //IEnumerable<Vertex> vertices =
                //    generator.HasRidge(neighborEdge) ? subregionVertices.Skip(direction * 5 + 1).Take(8) : subregionVertices.Skip(direction * 5 + 2).Take(6);

                if (generator.HasRidge(neighborEdge))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 v = subregionVertices[direction * 5 + 1 + i];
                        if (v != null)
                            sedge.AddVertex(v);
                    }

                    sedge.Center = Vector2.Lerp(subregionVertices[direction * 5 + 4], subregionVertices[direction * 5 + 5], 0.5);
                    sedge.AddVertex(sedge.Center);

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 v = subregionVertices[direction * 5 + 5 + i];
                        if (v != null)
                            sedge.AddVertex(v);
                    }
                }
                else
                {
                    Vector2 last = null;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 v = subregionVertices[direction * 5 + 2 + i];
                        if (v != null)
                        {
                            sedge.AddVertex(v);
                            last = v;
                        }                           
                    }

                    //sedge.Center = Vertex.Between(subregion.Cell.GetVertex(direction), subregion.Cell.GetVertex(direction + 1), 0.5);
                    //sedge.AddVertex(sedge.Center);

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 v = subregionVertices[direction * 5 + 5 + i];
                        if (v != null)
                        {
                            if (sedge.Center == null)
                            {
                                sedge.Center = Vector2.Lerp(last, v, 0.5);
                                sedge.AddVertex(sedge.Center);
                            }
                            sedge.AddVertex(v);
                        }
                            
                    }
                }            

                if (generator.HasRidge(neighborEdge))
                    subregion.AddNeighbor(_subregionByEdge[neighborEdge], sedge);
                else if (neighborCell != null)
                    subregion.AddNeighbor(_subregionByCell[neighborCell], sedge);
                else
                    subregion.AddNeighbor(null, sedge);
            }

            subregion.Center = cell.Center;
        }

        public void GenerateSpatialIndex(RandomExt random)
        {
            BoundingBox bbox = new BoundingBox(-Width * 0.25, -Height * 0.25, Width * 1.25, Height * 1.25);

            //SpatialIndex = new TrapezoidSpatialIndex<Subregion>(Subregions, bbox, 0, random);

            int capacity = 4;

            while (true)
            {
                try
                {
                    SpatialIndex = new QuadTreeSpatialIndex<TSubregion>(Subregions, bbox, capacity);
                    break;
                }
                catch
                {
                    capacity += 1;
                    Debug.WriteLine($"Increasing spatial index capacity to {capacity}");
                }
            }
        }

        private Vector2[] _cellRegionVertices(TGen generator, TCell cell, int direction)
        {
            Vector2[] vertices = new Vector2[5];
            TCell cellRight = cell.GetNeighbor(direction);
            TCell cellLeft = cell.GetNeighbor(direction + 5);
            Vector2 vertex = cell.GetVertex(direction);
            bool ridgeLeft = generator.HasRidge(cell.GetEdge(direction + 5));
            bool ridgeRight = generator.HasRidge(cell.GetEdge(direction));
            bool ridgeMiddle = cellRight != null && generator.HasRidge(cellRight.GetEdge(direction + 4));

            if (ridgeLeft)
                vertices[0] = Vector2.Lerp(vertex, cell.Center, _delta);
            if (ridgeRight)
                vertices[4] = Vector2.Lerp(vertex, cell.Center, _delta);

            if ((ridgeMiddle || ridgeRight) && !ridgeLeft)
            {
                vertices[1] = _smoothingVertexLeft(cell, direction);
            }
            if ((ridgeMiddle || ridgeLeft) && !ridgeRight)
            {
                vertices[3] = _smoothingVertexRight(cell, direction);
            }
            if (!ridgeMiddle && !ridgeLeft && !ridgeRight)
            {
                vertices[2] = new Vector2(vertex);
            }
            else if (ridgeMiddle && !ridgeLeft && !ridgeRight && _useRidgeSmoothing)
            {
                vertices[2] = Vector2.Lerp(vertex, cell.Center, 0.2);
            }

            if (cellLeft == null && cellRight != null && ridgeRight)
            {
                Vector2 v1 = Vector2.Lerp(vertex, cell.Center, _delta);
                Vector2 v2 = Vector2.Lerp(cell.GetVertex(direction + 1), cell.Center, _delta);
                Vector2 v3 = cell.GetVertex(direction + 5);

                vertices[1] = LineSegment.FindLineIntersection(v1, v2, vertex, v3);
                vertices[2] = null;
            }
            if (cellRight == null && cellLeft != null && ridgeLeft)
            {
                Vector2 v1 = Vector2.Lerp(vertex, cell.Center, _delta);
                Vector2 v2 = Vector2.Lerp(cell.GetVertex(direction + 5), cell.Center, _delta);
                Vector2 v3 = cell.GetVertex(direction + 1);

                vertices[3] = LineSegment.FindLineIntersection(v1, v2, vertex, v3);
                vertices[2] = null;
            }        

            return vertices;
        }

        private void _calculateGeometryRidge(TSubregion subregion, TGen generator)
        {
            Vector2[] vertices = _ridgeSubregionVertices(subregion.Edge, generator);
            _createRidgeSubregionEdges(subregion, generator, vertices);

            subregion.Center = Vector2.Lerp(subregion.Edge.Vertex1, subregion.Edge.Vertex2, 0.5);
        }

        private Vector2[] _ridgeSubregionVertices(TEdge edge, TGen generator)
        {
            TCell c1 = edge.Cell1;
            TCell c2 = edge.Cell2;
            int dir = c1.GetDirection(edge);

            TEdge edgeL = c1.GetEdge(dir + 5);
            TEdge edgeR = c1.GetEdge(dir + 1);
            TEdge edgeR1 = c2?.GetEdge(dir + 4);
            TEdge edgeL1 = c2?.GetEdge(dir + 2);

            TCell cellL = c1.GetNeighbor(dir + 5);
            TCell cellR = c1.GetNeighbor(dir + 1);

            Vector2[] vertices = new Vector2[10];

            vertices[0] = new Vector2(c1.GetVertex(dir + 1));
            vertices[5] = new Vector2(c1.GetVertex(dir));
            vertices[2] = Vector2.Lerp(vertices[0], c1.Center, _delta);
            vertices[3] = Vector2.Lerp(vertices[5], c1.Center, _delta);

            if (c2 != null)
            {
                vertices[7] = Vector2.Lerp(c2.GetVertex(dir + 4), c2.Center, _delta);
                vertices[8] = Vector2.Lerp(c2.GetVertex(dir + 3), c2.Center, _delta);
            }

            if (_useRidgeSmoothing)
            {
                if (generator.HasRidge(edge) && !generator.HasRidge(edgeL) && cellL != null)
                {
                    vertices[4] = _smoothingVertexLeft(c1, dir);
                }
                if (generator.HasRidge(edge) && !generator.HasRidge(edgeR) && cellR != null)
                {
                    vertices[1] = _smoothingVertexRight(c1, dir + 1);                    
                }
                if (generator.HasRidge(edge) && edgeR1 != null && !generator.HasRidge(edgeR1) && cellL != null)
                {
                    vertices[6] = _smoothingVertexRight(c2, dir + 4);
                }
                if (generator.HasRidge(edge) && edgeL1 != null && !generator.HasRidge(edgeL1) && cellR != null)
                {
                    vertices[9] = _smoothingVertexLeft(c2, dir + 3);
                }

                if (generator.HasRidge(edge) && !generator.HasRidge(edgeL) && edgeR1 != null && !generator.HasRidge(edgeR1) && cellL != null)
                {
                    Vector2 cL = new Vector2(cellL.Center);
                    cL = _normalizeVertex(cL, edge.Vertex1.X);
                    vertices[5] = Vector2.Lerp(vertices[5], cL, 0.2);
                }
                if (generator.HasRidge(edge) && !generator.HasRidge(edgeR) && edgeL1 != null && !generator.HasRidge(edgeL1) && cellR != null)
                {
                    Vector2 cR = new Vector2(cellR.Center);
                    cR =_normalizeVertex(cR, edge.Vertex1.X);
                    vertices[0] = Vector2.Lerp(vertices[0], cR, 0.2);
                }

                if (cellL == null)
                {
                    Vector2 v1 = c1.GetVertex(dir + 5);
                    Vector2 v2 = c2.GetVertex(dir + 5);
                    vertices[4] = LineSegment.FindLineIntersection(vertices[3], vertices[2], vertices[5], v1);
                    vertices[6] = LineSegment.FindLineIntersection(vertices[7], vertices[8], vertices[5], v2);
                }
                if (cellR == null)
                {
                    Vector2 v1 = c1.GetVertex(dir + 2);
                    Vector2 v2 = c2.GetVertex(dir + 2);
                    vertices[1] = LineSegment.FindLineIntersection(vertices[2], vertices[3], vertices[0], v1);
                    vertices[9] = LineSegment.FindLineIntersection(vertices[8], vertices[7], vertices[0], v2);
                }
            }         

            vertices = vertices.Select(v => v == null ? null : _normalizeVertex(v, edge.Vertex1.X)).ToArray();

            return vertices;
        }

        private void _createRidgeSubregionEdges(TSubregion subregion, TGen generator, Vector2[] vp)
        {
            TEdge edge = subregion.Edge;
            TCell cell1 = edge.Cell1;
            TCell cell2 = edge.Cell2;
            int dir = cell1.GetDirection(edge);

            Action<int, TEdge, TEdge, TCell> addEdgeBetweenRidgeAndCell = (index, leftEdge, rightEdge, cell) =>
            {
                SubregionEdge sEdge = new SubregionEdge();
                TCell otherCell = cell == cell1 ? cell2 : cell1;
                int direction = cell == cell1 ? dir : dir + 3;
                
                if (vp[(index + 1) % 10] != null && otherCell != null && !generator.HasRidge(otherCell.GetEdge(direction + 2)))
                    sEdge.AddVertex(vp[index]);

                if (!generator.HasRidge(rightEdge))
                    sEdge.AddVertex(vp[(index + 1) % 10] ?? vp[index]);
                sEdge.AddVertex(vp[(index + 2) % 10]);

                sEdge.Center = Vector2.Lerp(vp[(index + 2) % 10], vp[(index + 3) % 10], 0.5);
                sEdge.AddVertex(sEdge.Center);

                sEdge.AddVertex(vp[(index + 3) % 10]);
                if (!generator.HasRidge(leftEdge))
                    sEdge.AddVertex(vp[(index + 4) % 10] ?? vp[(index + 5) % 10]);

                if (vp[(index + 4) % 10] != null && otherCell != null && !generator.HasRidge(otherCell.GetEdge(direction + 4)))
                    sEdge.AddVertex(vp[(index + 5) % 10]);

                subregion.AddEdge(sEdge);
                subregion.AddNeighbor(_subregionByCell[cell], sEdge);
            };

            Action<bool, int, TEdge> addEdgeBetweenRidges = (isLeft, index, neighborEdge) =>
            {
                if (generator.HasRidge(neighborEdge))
                {
                    SubregionEdge se = new SubregionEdge();
                    if (!isLeft && vp[(index + 9) % 10] != null)
                        se.AddVertex(vp[(index + 9) % 10]);
                    se.AddVertex(vp[index]);
                    se.AddVertex(vp[(index + 2) % 10]);
                    if (isLeft && vp[(index + 3) % 10] != null)
                        se.AddVertex(vp[(index + 3) % 10]);
                    subregion.AddEdge(se);
                    subregion.AddNeighbor(_subregionByEdge[neighborEdge], se);
                }                
            };

            TEdge edgeRight1 = cell1.GetEdge(dir + 1);
            TEdge edgeLeft1 = cell1.GetEdge(dir + 5);
            addEdgeBetweenRidges(false, 0, edgeRight1);
            addEdgeBetweenRidgeAndCell(0, edgeLeft1, edgeRight1, cell1);
            addEdgeBetweenRidges(true, 3, edgeLeft1);

            if (cell2 != null)
            {
                TEdge edgeRight2 = cell2.GetEdge(dir + 4);
                TEdge edgeLeft2 = cell2.GetEdge(dir + 2);
                addEdgeBetweenRidges(false, 5, edgeRight2);
                addEdgeBetweenRidgeAndCell(5, edgeLeft2, edgeRight2, cell2);
                addEdgeBetweenRidges(true, 8, edgeLeft2);
            }                   
        }       

        private void _createCellSubregion(TCell cell, Dictionary<TCell, TSubregion> subregionByCell, TGen generator)
        {
            TSubregion subregion = new TSubregion();
            subregion.Type = SubregionType.Cell;
            subregion.Cell = cell;
            subregion.ParentCell = generator.GetCellParent(cell);
            subregionByCell[cell] = subregion;
            _subregions.Add(subregion);
        }

        private void _createEdgeSubregion(TEdge edge, Dictionary<TEdge, TSubregion> subregionByEdge, IGeneratorEdge<TEdge> generator)
        {
            TSubregion subregion = new TSubregion();
            subregion.Type = SubregionType.Edge;
            subregion.Edge = edge;
            subregion.ParentEdge = generator.GetEdgeParent(edge);
            subregionByEdge[edge] = subregion;
            _subregions.Add(subregion);
        }

        Vector2 _smoothingVertexLeft(TCell cell, int direction)
        {
            if (cell == null)
                return null;

            TCell cellL = cell.GetNeighbor(direction + 5);

            if (cellL == null)
                return null;

            Vector2 vertex = cell.GetVertex(direction);
            Vector2 vertexI = Vector2.Lerp(vertex, cell.Center, _delta);
            Vector2 vertexO = cell.GetVertex(direction + 5);

            Vector2 cellCenterL = new Vector2(cellL.Center);
            cellCenterL = _normalizeVertex(cellCenterL, vertex.X);
            Vector2 vertexL = Vector2.Lerp(vertex, cellCenterL, _delta);

            return LineSegment.FindLineIntersection(vertex, vertexO, vertexI, vertexL);
        }

        Vector2 _smoothingVertexRight(TCell cell, int direction)
        {
            if (cell == null)
                return null;

            TCell cellR = cell.GetNeighbor(direction);

            if (cellR == null)
                return null;

            Vector2 vertex = cell.GetVertex(direction);
            Vector2 vertexI = Vector2.Lerp(vertex, cell.Center, _delta);
            Vector2 vertexO = cell.GetVertex(direction + 1);

            Vector2 cellCenterR = new Vector2(cellR.Center);
            cellCenterR = _normalizeVertex(cellCenterR, vertex.X);
            Vector2 vertexR = Vector2.Lerp(vertex, cellCenterR, _delta);

            return LineSegment.FindLineIntersection(vertex, vertexO, vertexI, vertexR);
        }

        private Vector2 _normalizeVertex(Vector2 vertex, double edgeX)
        {
            double x = vertex.X;
            double halfWidth = Width / 2;
            if (x - edgeX > halfWidth)
            {
                x -= Width;
            }
            else if (edgeX - x > halfWidth)
            {
                x += Width;
            }
            return new Vector2(x, vertex.Y);
        }
    }
}
