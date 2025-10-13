using System.Reflection;
using System.Runtime.ExceptionServices;
using static System.Linq.Enumerable;

namespace Topology
{
    /// <summary>
    /// Representation of a grid of hexagons. Gives access to contained hexagons and edges.
    /// </summary>
    /// <typeparam name="TCell">Hexagon class.</typeparam>
    /// <typeparam name="TEdge">Edge class.</typeparam>
    public class HexGrid<TCell, TEdge>
        where TCell : HexCell<TCell, TEdge>, new()
        where TEdge : Edge<TCell>, new()
    {
        TCell[,] _cells;
        List<TEdge> _edges = [];

        static double _hexSide = 1;
        static double _hexWidth = _hexSide * Math.Sqrt(3);
        static double _hexHalfSide = _hexSide / 2;

        public HexGrid(int columns, int rows)
        {
            _cells = new TCell[columns, rows];

            foreach (var c in _coords) _createCell(c.x, c.y);
            foreach (var c in _coords) _linkToNeighbors(c.x, c.y);
            foreach (var c in _coords) _createVertices(c.x, c.y);
            foreach (var c in _coords) _linkVertices(c.x, c.y);
            foreach (var c in _coords) _createEdges(c.x, c.y);
        }

        public int Columns => _cells.GetLength(0);
        public int Rows => _cells.GetLength(1);
        public double Width => Columns * _hexWidth;
        public double Height => (Rows + 1.0 / 3) * 1.5 * _hexSide;
        public TCell GetCell(int x, int y) => _cells[x, y];
        public double HexSide => _hexSide;
        public IEnumerable<TCell> Cells => _coords.Select(c => GetCell(c.x, c.y));
        public IEnumerable<TEdge> Edges => _edges;
        public int CellCount => _cells.Length;

        /// <summary>
        /// Update all vertices based on vertex context and a formula provided by the caller.
        /// </summary>
        /// <param name="formula"></param>
        public void UpdateVertices(Func<Vector2, TCell, TCell, TCell, Vector2> formula)
        {
            foreach ((int x, int y) in _coords)
            {
                TCell cell = _cells[x, y];
                for (int direction = 0; direction < 6; direction++)
                {
                    if (_responsible(x, y, direction))
                    {
                        Vector2 updated = formula(cell.GetVertex(direction), cell, cell.GetNeighbor(direction + 5), cell.GetNeighbor(direction));
                        cell.GetVertex(direction).X = updated.X;
                        cell.GetVertex(direction).Y = updated.Y;
                    }
                }
            }
        }

        public override string ToString() => $"Grid [{Columns}, {Rows}]";

        IEnumerable<(int x, int y)> _coords => Range(0, Rows).SelectMany(y => Range(0, Columns).Select(x => (x, y)));

        void _createCell(int x, int y)
        {
            double centerX = (x + 0.5 * (1 + y % 2)) * _hexWidth;
            double centerY = (y * 3 + 2) * _hexHalfSide;
            _cells[x, y] = new TCell() { Center = new Vector2(centerX, centerY) };
        }

        void _linkToNeighbors(int x, int y)
        {
            int evenRow = y % 2;

            TCell cell = _cells[x, y];

            TCell? neNeighbor = _getCell(x + evenRow, y - 1);
            TCell? eeNeighbor = _getCell(x + 1, y);
            TCell? seNeighbor = _getCell(x + evenRow, y + 1);

            _linkTwoCells(cell, neNeighbor, 0);
            _linkTwoCells(cell, eeNeighbor, 1);
            _linkTwoCells(cell, seNeighbor, 2);
        }

        TCell? _getCell(int x, int y)
        {
            // warped by axis x but not by axis y
            x = x == Columns ? 0 : x;
            return (y < 0 || y >= Rows) ? null : _cells[x, y];
        }

        void _linkTwoCells(TCell cell1, TCell? cell2, int direction)
        {
            if (cell2 != null)
            {
                cell1.AddNeighbor(cell2, direction);
                cell2.AddNeighbor(cell1, direction + 3);
            }
        }

        // determines if the cell is responsible for the vertex when vertices are shared
        // if vertices are not shared, every cell is responsible for all of its vertices (i.e. when Vector2 is struct)
        bool _responsible(int x, int y, int dir)
        {
            return dir switch
            {
                1 => y == 0 || (x == Columns - 1 && y % 2 == 1),
                2 => y == Rows - 1 || (x == Columns - 1 && y % 2 == 1),
                4 => y == Rows - 1 || (x == 0 && y % 2 != 1),
                5 => y == 0 || (x == 0 && y % 2 != 1),
                _ => true
            };
        }

        //returns vertex reference when another cell is responsible for the vertex
        Vector2 _findVertex(TCell cell, int dir)
        {
            return dir switch
            {
                1 => cell.GetNeighbor(0).GetVertex(3),
                2 => cell.GetNeighbor(2).GetVertex(0),
                4 => cell.GetNeighbor(3).GetVertex(0),
                5 => cell.GetNeighbor(5).GetVertex(3),
                _ => throw new Exception()
            };
        }

        Vector2 _defaultVertex(TCell cell, int direction)
        {
            if (cell.Center == null) throw new Exception();
            return direction switch
            {
                0 => new Vector2(cell.Center.X, cell.Center.Y - _hexSide),
                1 => new Vector2(cell.Center.X + 0.5 * _hexWidth, cell.Center.Y - _hexHalfSide),
                2 => new Vector2(cell.Center.X + 0.5 * _hexWidth, cell.Center.Y + _hexHalfSide),
                3 => new Vector2(cell.Center.X, cell.Center.Y + _hexSide),
                4 => new Vector2(cell.Center.X - 0.5 * _hexWidth, cell.Center.Y + _hexHalfSide),
                5 => new Vector2(cell.Center.X - 0.5 * _hexWidth, cell.Center.Y - _hexHalfSide),
                _ => throw new Exception()
            };
        }

        void _createVertices(int x, int y)
        {  
            TCell cell = _cells[x, y];
            for (int direction = 0; direction < 6; direction++)
                if (_responsible(x, y, direction)) 
                    cell.SetVertex(_defaultVertex(cell, direction), direction);
        }

        void _linkVertices(int x, int y)
        {
            TCell cell = _cells[x, y];
            for (int direction = 0; direction < 6; direction++)
                if (!_responsible(x, y, direction))
                    cell.SetVertex(_findVertex(cell, direction), direction);
        }

        void _createEdges(int x, int y)
        {
            TCell cell = _cells[x, y];
            foreach (int i in Range(0, 6).Where(i => i < 3 || cell.GetNeighbor(i) == null)) 
                _createEdge(cell, i);
        }

        TEdge _createEdge(TCell cell, int direction)
        {
            TCell neighbor = cell.GetNeighbor(direction);
            TEdge edge = new TEdge() { 
                Cell1 = cell, Cell2 = neighbor, 
                Vertex1 = cell.GetVertex(direction),
                Vertex2 = cell.GetVertex(direction + 1) };
            cell.AddEdge(edge, direction);
            neighbor?.AddEdge(edge, direction + 3);
            _edges.Add(edge);
            return edge;
        }
    }

    /// <summary>
    /// Implementaion of the generic HexGrid class.
    /// </summary>
    public class HexGrid : HexGrid<HexCell, Edge>
    {
        public HexGrid(int _width, int _height) : base(_width, _height) { }
    }
}
