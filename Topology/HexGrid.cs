using static System.Linq.Enumerable;

namespace Topology
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TCell"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    public class HexGrid<TCell, TEdge>
        where TCell : HexCell<TCell, TEdge>, new()
        where TEdge : Edge<TCell>, new()
    {
        TCell[,] _cells;
        List<TEdge> _edges = [];

        static double _hexSide = 1;
        static double _hexWidth = _hexSide * Math.Sqrt(3);
        static double _hexHalfSide = _hexSide / 2;

        public HexGrid(int _width, int _height)
        {
            _cells = new TCell[_width, _height];

            _runForEachCell(_createCell);
            _runForEachCell(_designateNeighbors);
            _runForEachCell(_addVertices);
            _runForEachCell(_createEdges);
        }

        public int Columns => _cells.GetLength(0);
        public int Rows => _cells.GetLength(1);
        public double Width => Columns * _hexWidth;
        public double Height => (Rows + 1.0 / 3) * 1.5 * _hexSide;
        public TCell GetCell(int x, int y) => _cells[x, y];
        public double HexSide => _hexSide;
        public IEnumerable<TCell> Cells => Range(0, Rows).SelectMany(y => Range(0, Columns).Select(x => GetCell(x, y)));
        public IEnumerable<TEdge> Edges => _edges;
        public int CellCount => _cells.Length;

        void _runForEachCell(Action<int, int> method)
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    method(x, y);
                }
            }
        }

        void _createCell(int x, int y)
        {
            TCell cell = new TCell();
            _cells[x, y] = cell;

            int evenRow = y % 2;
            double centerX = (x + 0.5 + 0.5 * evenRow) * _hexWidth;
            double centerY = (y * 3 + 2) * _hexHalfSide;

            cell.Center = new Vector2(centerX, centerY);
        }

        void _designateNeighbors(int x, int y)
        {
            int evenRow = y % 2;

            TCell cell = _cells[x, y];

            TCell? neNeighbor = _getCell(x + evenRow, y - 1);
            TCell? eNeighbor = _getCell(x + 1, y);
            TCell? seNeighbor = _getCell(x + evenRow, y + 1);

            _markAsNeighbors(cell, neNeighbor, 0);
            _markAsNeighbors(cell, eNeighbor, 1);
            _markAsNeighbors(cell, seNeighbor, 2);
        }

        TCell? _getCell(int x, int y)
        {
            // warped by axis x but not by axis y
            x = x == Columns ? 0 : x;
            return (y < 0 || y >= Rows) ? null : _cells[x, y];
        }

        void _markAsNeighbors(TCell cell1, TCell? cell2, int direction)
        {
            if (cell2 != null)
            {
                cell1.AddNeighbor(cell2, direction);
                cell2.AddNeighbor(cell1, direction + 3);
            }
        }

        void _addVertices(int x, int y)
        {
            TCell cell = _cells[x, y];
            int evenRow = y % 2;

            if (cell.Center == null) throw new Exception();

            Vector2 up = _addVertexToCell(cell, 0, cell.Center.X, cell.Center.Y - _hexSide);
            Vector2 down = _addVertexToCell(cell, 3, cell.Center.X, cell.Center.Y + _hexSide);

            if (y == 0 || (x == Columns - 1 && evenRow == 1))
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

            if (y == Rows - 1 || (x == Columns - 1 && evenRow == 1))
            {
                _addVertexToCell(cell, 2, cell.Center.X + 0.5 * _hexWidth, cell.Center.Y + _hexHalfSide);
            }
            else
            {
                cell.GetNeighbor(2).AddVertex(down, 5);
            }

            if (y == Rows - 1 || (x == 0 && evenRow == 0))
            {
                _addVertexToCell(cell, 4, cell.Center.X - 0.5 * _hexWidth, cell.Center.Y + _hexHalfSide);
            }
            else
            {
                cell.GetNeighbor(3).AddVertex(down, 1);
            }
        }

        Vector2 _addVertexToCell(TCell cell, int direction, double x, double y)
        {
            Vector2 vertex = new Vector2(x, y);
            cell.AddVertex(vertex, direction);
            return vertex;
        }

        void _createEdges(int x, int y)
        {
            TCell cell = _cells[x, y];

            _createEdge(cell, 0);
            _createEdge(cell, 1);
            _createEdge(cell, 2);

            for (int i = 3; i < 6; i++)
            {
                if (cell.GetNeighbor(i) == null)
                {
                    TEdge edge = _createEdge(cell, i);
                }
            }
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

    public class HexGrid : HexGrid<HexCell, Edge>
    {
        public HexGrid(int _width, int _height) : base(_width, _height) { }
    }
}
