using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexGrid
{
    public class Grid
    {
        HexCell[,] _cells;
        List<Edge> _edges = new List<Edge>();

        static double _hexSide = 1;
        static double _hexWidth = _hexSide * Math.Sqrt(3);
        static double _hexHalfSide = _hexSide / 2;

        public static IEnumerable<T> Flood<T>(T starterCell, Func<T, bool> canBeAdded)
            where T: INeighbors<T>
        {
            HashSet<T> floodedCells = new HashSet<T>();
            HashSet<T> inProcess = new HashSet<T>();
            inProcess.Add(starterCell);

            Func<T, bool> isNotProcessed = c => !(floodedCells.Contains(c) || inProcess.Contains(c));

            while (inProcess.Count > 0)
            {
                HashSet<T> waiting = new HashSet<T>();
                foreach (T cell in inProcess)
                {
                    floodedCells.Add(cell);
                    waiting.UnionWith(cell.Neighbors.Where(canBeAdded).Where(isNotProcessed));
                }
                inProcess = waiting;
            }

            return floodedCells;
        }

        public static bool IsConnection<T>(HexCell cell, Func<HexCell, T> value)
        {
            HashSet<HexCell> sameNeighbors = cell.Neighbors.Where(c => value(c).Equals(value(cell))).ToHashSet();
            if (sameNeighbors.Count == 0)
            {
                return false;
            }
            HexCell starterCell = sameNeighbors.First();
            IEnumerable<HexCell> connectedNeighbors = Grid.Flood(starterCell, sameNeighbors.Contains);
            return sameNeighbors.Count > connectedNeighbors.Count();
        }


        public Grid(int _width, int _height)
        {
            Width = _width;
            Height = _height;

            _cells = new HexCell[Width, Height];

            _runForEachCell(_createCell);
            _runForEachCell(_designateNeighbors);
            _runForEachCell(_addVertices);
            _runForEachCell(_createEdges);
        }

        public int Width { get; }
        public int Height { get; }
        public double XDimension => Width * _hexWidth;
        public double YDimension => (Height + 1.0 / 3) * 1.5 * _hexSide;
        public HexCell GetCell(int x, int y) => _cells[x, y];
        public double HexSide => _hexSide;
        public IEnumerable<HexCell> Cells
        {
            get
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        yield return _cells[x, y];
                    }
                }
            }
        }
        public IEnumerable<Edge> Edges => _edges;
        public int CellCount => Width * Height;

        void _runForEachCell(Action<int, int> method)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    method(x, y);
                }
            }
        }

        void _createCell(int x, int y)
        {
            HexCell cell = new HexCell();
            _cells[x, y] = cell;

            int evenRow = y % 2;
            double centerX = (x + 0.5 + 0.5 * evenRow) * _hexWidth;
            double centerY = (y * 3 + 2) * _hexHalfSide;

            cell.Center = new Vertex(centerX, centerY);
            cell.GridPositionX = x;
            cell.GridPositionY = y;
        }

        void _designateNeighbors(int x, int y)
        {
            int evenRow = y % 2;

            HexCell cell = _cells[x, y];

            HexCell neNeighbor = _getCell(x + evenRow, y - 1);
            HexCell eNeighbor = _getCell(x + 1, y);
            HexCell seNeighbor = _getCell(x + evenRow, y + 1);

            _markAsNeighbors(cell, neNeighbor, 0);
            _markAsNeighbors(cell, eNeighbor, 1);
            _markAsNeighbors(cell, seNeighbor, 2);
        }

        HexCell _getCell(int x, int y)
        {
            if (x == -1)
            {
                x = Width - 1;
            }
            else if (x == Width)
            {
                x = 0;
            }

            if (y < 0 || y >= Height)
            {
                return null;
            }

            return _cells[x, y];
        }

        void _markAsNeighbors(HexCell cell1, HexCell cell2, int direction)
        {
            if (cell2 != null)
            {
                cell1.AddNeighbor(cell2, direction);
                cell2.AddNeighbor(cell1, direction + 3);
            }
        }

        void _addVertices(int x, int y)
        {
            HexCell cell = _cells[x, y];
            int evenRow = y % 2;

            Vertex up = _addVertexToCell(cell, 0, cell.Center.X, cell.Center.Y - _hexSide);
            Vertex down = _addVertexToCell(cell, 3, cell.Center.X, cell.Center.Y + _hexSide);

            if (y == 0 || (x == Width - 1 && evenRow == 1))
            {
                _addVertexToCell(cell, 1, cell.Center.X + 0.5 * _hexWidth, cell.Center.Y - _hexHalfSide);
            }
            else
            {
                cell.GetNeighbor(0).AddVertex(up, 4);
            }

            if (y == 0 || (x == 0 && evenRow == 0))
            {
                _addVertexToCell(cell, 5, cell.Center.X - 0.5 * _hexWidth, cell.Center.Y - _hexHalfSide);
            }
            else
            {
                cell.GetNeighbor(5).AddVertex(up, 2);
            }

            if (y == Height - 1 || (x == Width - 1 && evenRow == 1))
            {
                _addVertexToCell(cell, 2, cell.Center.X + 0.5 * _hexWidth, cell.Center.Y + _hexHalfSide);
            }
            else
            {
                cell.GetNeighbor(2).AddVertex(down, 5);
            }

            if (y == Height - 1 || (x == 0 && evenRow == 0))
            {
                _addVertexToCell(cell, 4, cell.Center.X - 0.5 * _hexWidth, cell.Center.Y + _hexHalfSide);
            }
            else
            {
                cell.GetNeighbor(3).AddVertex(down, 1);
            }
        }

        Vertex _addVertexToCell(HexCell cell, int direction, double x, double y)
        {
            Vertex vertex = new Vertex(x, y);
            cell.AddVertex(vertex, direction);
            return vertex;
        }

        void _createEdges(int x, int y)
        {
            HexCell cell = _cells[x, y];

            _createEdge(cell, 0);
            _createEdge(cell, 1);
            _createEdge(cell, 2);

            for (int i = 3; i < 6; i++)
            {
                if (cell.GetNeighbor(i) == null)
                {
                    Edge edge = _createEdge(cell, i);
                }
            }
        }

        Edge _createEdge(HexCell cell, int direction)
        {
            HexCell neighbor = cell.GetNeighbor(direction);
            Edge edge = new Edge() { Cell1 = cell, Cell2 = neighbor, Vertex1 = cell.GetVertex(direction), Vertex2 = cell.GetVertex((direction + 1) % 6) };
            cell.AddEdge(edge, direction);
            neighbor?.AddEdge(edge, direction + 3);
            _edges.Add(edge);
            return edge;
        }
    }
}
