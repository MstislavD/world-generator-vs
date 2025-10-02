using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;
using PointLocation;
using RandomExtension;

namespace WorldSimulation
{
    public enum SubregionType { Cell, Edge }

    public class SubregionGraph
    {
        List<Subregion> _subregions = new List<Subregion>();
        Dictionary<Edge, Subregion> _subregionByEdge = new Dictionary<Edge, Subregion>();
        Dictionary<HexCell, Subregion> _subregionByCell = new Dictionary<HexCell, Subregion>();
        Dictionary<HexCell, List<Subregion>> _subregionsByCellRegion = new Dictionary<HexCell, List<Subregion>>();
        Dictionary<Edge, List<Subregion>> _subregionsByEdgeRegion = new Dictionary<Edge, List<Subregion>>();

        double _delta = 0.5;
        const bool _useRidgeSmoothing = true;

        public SubregionGraph(Grid grid, WorldGenerator generator)
        {
            Width = grid.XDimension;
            Height = grid.YDimension;

            foreach (Edge edge in grid.Edges.Where(generator.HasRidge))
            {
               _createEdgeSubregion(edge, _subregionByEdge, generator);
            }

            foreach (HexCell cell in grid.Cells)
            {
                _createCellSubregion(cell, _subregionByCell, generator);
            }

            foreach (Subregion subregion in _subregions.Where(sr => sr.Type == SubregionType.Edge))
            {
                _calculateGeometryRidge(subregion, generator);

                if (!_subregionsByEdgeRegion.ContainsKey(subregion.ParentEdge))
                {
                    _subregionsByEdgeRegion[subregion.ParentEdge] = new List<Subregion>();
                }
                _subregionsByEdgeRegion[subregion.ParentEdge].Add(subregion);
            }

            foreach (Subregion subregion in _subregions.Where(sr => sr.Type == SubregionType.Cell))
            {
                _calculateGeometryCell(subregion, generator);

                if (!_subregionsByCellRegion.ContainsKey(subregion.ParentCell))
                {
                    _subregionsByCellRegion[subregion.ParentCell] = new List<Subregion>();
                }
                _subregionsByCellRegion[subregion.ParentCell].Add(subregion);
            }
        }

        public IEnumerable<Subregion> Subregions => _subregions;
        public double Width { get; }
        public double Height { get; }
        public IEnumerable<Subregion> CellSubregions => _subregions.Where(sr => sr.Type == SubregionType.Cell);
        public IEnumerable<Subregion> EdgeSubregions => _subregions.Where(sr => sr.Type == SubregionType.Edge);
        public Subregion GetSubregion(HexCell cell) => _subregionByCell[cell];
        public PointLocator<Subregion> Locator { get; private set; }
        public IEnumerable<Subregion> GetSubregions(HexCell region) => _subregionsByCellRegion[region];
        public IEnumerable<Subregion> GetSubregions(Edge edge) => _subregionsByEdgeRegion[edge];

        private void _calculateGeometryCell(Subregion subregion, WorldGenerator generator)
        {
            HexCell cell = subregion.Cell;
            List<Vector2> subregionVertices = new List<Vector2>();

            for (int direction = 0; direction < 7; direction++)
            {
                Vector2[] vertices = _cellRegionVertices(generator, cell, direction);
                subregionVertices.AddRange(vertices);
            }

            for (int direction = 0; direction < 6; direction++)
            {
                SubregionEdge sedge = new SubregionEdge();
                HexCell neighborCell = cell.GetNeighbor(direction);
                Edge neighborEdge = cell.GetEdge(direction);
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

                    sedge.Center = Vector2.Between(subregionVertices[direction * 5 + 4], subregionVertices[direction * 5 + 5], 0.5);
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
                                sedge.Center = Vector2.Between(last, v, 0.5);
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

        public void GenerateLocator(RandomExt random) => Locator = new PointLocator<Subregion>(new SubregionPartition(this), random);

        private Vector2[] _cellRegionVertices(WorldGenerator generator, HexCell cell, int direction)
        {
            if (cell.GridPositionX == 31 && cell.GridPositionY == 48)
            {

            }

            Vector2[] vertices = new Vector2[5];
            HexCell cellRight = cell.GetNeighbor(direction);
            HexCell cellLeft = cell.GetNeighbor(direction + 5);
            Vector2 vertex = cell.GetVertex(direction);
            bool ridgeLeft = generator.HasRidge(cell.GetEdge(direction + 5));
            bool ridgeRight = generator.HasRidge(cell.GetEdge(direction));
            bool ridgeMiddle = cellRight != null && generator.HasRidge(cellRight.GetEdge(direction + 4));

            if (ridgeLeft)
                vertices[0] = Vector2.Between(vertex, cell.Center, _delta);
            if (ridgeRight)
                vertices[4] = Vector2.Between(vertex, cell.Center, _delta);

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
                vertices[2] = Vector2.Between(vertex, cell.Center, 0.2);
            }

            if (cellLeft == null && cellRight != null && ridgeRight)
            {
                Vector2 v1 = Vector2.Between(vertex, cell.Center, _delta);
                Vector2 v2 = Vector2.Between(cell.GetVertex(direction + 1), cell.Center, _delta);
                Vector2 v3 = cell.GetVertex(direction + 5);

                vertices[1] = Vector2.Crossing(v1, v2, vertex, v3);
                vertices[2] = null;
            }
            if (cellRight == null && cellLeft != null && ridgeLeft)
            {
                Vector2 v1 = Vector2.Between(vertex, cell.Center, _delta);
                Vector2 v2 = Vector2.Between(cell.GetVertex(direction + 5), cell.Center, _delta);
                Vector2 v3 = cell.GetVertex(direction + 1);

                vertices[3] = Vector2.Crossing(v1, v2, vertex, v3);
                vertices[2] = null;
            }        

            return vertices;
        }

        private void _calculateGeometryRidge(Subregion subregion, WorldGenerator generator)
        {
            Vector2[] vertices = _ridgeSubregionVertices(subregion.Edge, generator);
            _createRidgeSubregionEdges(subregion, generator, vertices);

            subregion.Center = Vector2.Between(subregion.Edge.Vertex1, subregion.Edge.Vertex2, 0.5);
        }

        private Vector2[] _ridgeSubregionVertices(Edge edge, WorldGenerator generator)
        {
            HexCell c1 = edge.Cell1;
            HexCell c2 = edge.Cell2;
            int dir = c1.GetDirection(edge);

            Edge edgeL = c1.GetEdge(dir + 5);
            Edge edgeR = c1.GetEdge(dir + 1);
            Edge edgeR1 = c2?.GetEdge(dir + 4);
            Edge edgeL1 = c2?.GetEdge(dir + 2);

            HexCell cellL = c1.GetNeighbor(dir + 5);
            HexCell cellR = c1.GetNeighbor(dir + 1);

            Vector2[] vertices = new Vector2[10];

            vertices[0] = new Vector2(c1.GetVertex(dir + 1));
            vertices[5] = new Vector2(c1.GetVertex(dir));
            vertices[2] = Vector2.Between(vertices[0], c1.Center, _delta);
            vertices[3] = Vector2.Between(vertices[5], c1.Center, _delta);

            if (c2 != null)
            {
                vertices[7] = Vector2.Between(c2.GetVertex(dir + 4), c2.Center, _delta);
                vertices[8] = Vector2.Between(c2.GetVertex(dir + 3), c2.Center, _delta);
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
                    _normalizeVertex(cL, edge.Vertex1.X);
                    vertices[5] = Vector2.Between(vertices[5], cL, 0.2);
                }
                if (generator.HasRidge(edge) && !generator.HasRidge(edgeR) && edgeL1 != null && !generator.HasRidge(edgeL1) && cellR != null)
                {
                    Vector2 cR = new Vector2(cellR.Center);
                    _normalizeVertex(cR, edge.Vertex1.X);
                    vertices[0] = Vector2.Between(vertices[0], cR, 0.2);
                }

                if (cellL == null)
                {
                    Vector2 v1 = c1.GetVertex(dir + 5);
                    Vector2 v2 = c2.GetVertex(dir + 5);
                    vertices[4] = Vector2.Crossing(vertices[3], vertices[2], vertices[5], v1);
                    vertices[6] = Vector2.Crossing(vertices[7], vertices[8], vertices[5], v2);
                }
                if (cellR == null)
                {
                    Vector2 v1 = c1.GetVertex(dir + 2);
                    Vector2 v2 = c2.GetVertex(dir + 2);
                    vertices[1] = Vector2.Crossing(vertices[2], vertices[3], vertices[0], v1);
                    vertices[9] = Vector2.Crossing(vertices[8], vertices[7], vertices[0], v2);
                }
            }         

            foreach (Vector2 vertex in vertices.Where(v => v != null))
            {
                _normalizeVertex(vertex, edge.Vertex1.X);
            }

            return vertices;
        }

        private void _createRidgeSubregionEdges(Subregion subregion, WorldGenerator generator, Vector2[] vp)
        {
            Edge edge = subregion.Edge;
            HexCell cell1 = edge.Cell1;
            HexCell cell2 = edge.Cell2;
            int dir = cell1.GetDirection(edge);

            Action<int, Edge, Edge, HexCell> addEdgeBetweenRidgeAndCell = (index, leftEdge, rightEdge, cell) =>
            {
                SubregionEdge sEdge = new SubregionEdge();
                HexCell otherCell = cell == cell1 ? cell2 : cell1;
                int direction = cell == cell1 ? dir : dir + 3;
                
                if (vp[(index + 1) % 10] != null && otherCell != null && !generator.HasRidge(otherCell.GetEdge(direction + 2)))
                    sEdge.AddVertex(vp[index]);

                if (!generator.HasRidge(rightEdge))
                    sEdge.AddVertex(vp[(index + 1) % 10] ?? vp[index]);
                sEdge.AddVertex(vp[(index + 2) % 10]);

                sEdge.Center = Vector2.Between(vp[(index + 2) % 10], vp[(index + 3) % 10], 0.5);
                sEdge.AddVertex(sEdge.Center);

                sEdge.AddVertex(vp[(index + 3) % 10]);
                if (!generator.HasRidge(leftEdge))
                    sEdge.AddVertex(vp[(index + 4) % 10] ?? vp[(index + 5) % 10]);

                if (vp[(index + 4) % 10] != null && otherCell != null && !generator.HasRidge(otherCell.GetEdge(direction + 4)))
                    sEdge.AddVertex(vp[(index + 5) % 10]);

                subregion.AddEdge(sEdge);
                subregion.AddNeighbor(_subregionByCell[cell], sEdge);
            };

            Action<bool, int, Edge> addEdgeBetweenRidges = (isLeft, index, neighborEdge) =>
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

            Edge edgeRight1 = cell1.GetEdge(dir + 1);
            Edge edgeLeft1 = cell1.GetEdge(dir + 5);
            addEdgeBetweenRidges(false, 0, edgeRight1);
            addEdgeBetweenRidgeAndCell(0, edgeLeft1, edgeRight1, cell1);
            addEdgeBetweenRidges(true, 3, edgeLeft1);

            if (cell2 != null)
            {
                Edge edgeRight2 = cell2.GetEdge(dir + 4);
                Edge edgeLeft2 = cell2.GetEdge(dir + 2);
                addEdgeBetweenRidges(false, 5, edgeRight2);
                addEdgeBetweenRidgeAndCell(5, edgeLeft2, edgeRight2, cell2);
                addEdgeBetweenRidges(true, 8, edgeLeft2);
            }                   
        }       

        private void _createCellSubregion(HexCell cell, Dictionary<HexCell, Subregion> subregionByCell, WorldGenerator generator)
        {
            Subregion subregion = new Subregion();
            subregion.Type = SubregionType.Cell;
            subregion.Cell = cell;
            subregion.ParentCell = generator.GetCellParent(cell);
            subregionByCell[cell] = subregion;
            _subregions.Add(subregion);
        }

        private void _createEdgeSubregion(Edge edge, Dictionary<Edge, Subregion> subregionByEdge, WorldGenerator generator)
        {
            Subregion subregion = new Subregion();
            subregion.Type = SubregionType.Edge;
            subregion.Edge = edge;
            subregion.ParentEdge = generator.GetEdgeParent(edge);
            subregionByEdge[edge] = subregion;
            _subregions.Add(subregion);
        }

        Vector2 _smoothingVertexLeft(HexCell cell, int direction)
        {
            if (cell == null)
                return null;

            HexCell cellL = cell.GetNeighbor(direction + 5);

            if (cellL == null)
                return null;

            Vector2 vertex = cell.GetVertex(direction);
            Vector2 vertexI = Vector2.Between(vertex, cell.Center, _delta);
            Vector2 vertexO = cell.GetVertex(direction + 5);

            Vector2 cellCenterL = new Vector2(cellL.Center);
            _normalizeVertex(cellCenterL, vertex.X);
            Vector2 vertexL = Vector2.Between(vertex, cellCenterL, _delta);

            return Vector2.Crossing(vertex, vertexO, vertexI, vertexL);
        }

        Vector2 _smoothingVertexRight(HexCell cell, int direction)
        {
            if (cell == null)
                return null;

            HexCell cellR = cell.GetNeighbor(direction);

            if (cellR == null)
                return null;

            Vector2 vertex = cell.GetVertex(direction);
            Vector2 vertexI = Vector2.Between(vertex, cell.Center, _delta);
            Vector2 vertexO = cell.GetVertex(direction + 1);

            Vector2 cellCenterR = new Vector2(cellR.Center);
            _normalizeVertex(cellCenterR, vertex.X);
            Vector2 vertexR = Vector2.Between(vertex, cellCenterR, _delta);

            return Vector2.Crossing(vertex, vertexO, vertexI, vertexR);
        }

        private void _normalizeVertex(Vector2 vertex, double edgeX)
        {
            double halfWidth = Width / 2;
            if (vertex.X - edgeX > halfWidth)
            {
                vertex.X -= Width;
            }
            else if (edgeX - vertex.X > halfWidth)
            {
                vertex.X += Width;
            }
        }
    }
}
