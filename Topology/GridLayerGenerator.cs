using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Topology
{
    public interface ILayerCell<TCell>
    {
        List<TCell>? Children { get; }
        TCell? Parent { get; set; }
        void AddChild(TCell cell);
    }

    public class LayerHexCell<TCell, TEdge> : HexCell<TCell, TEdge>, ILayerCell<TCell>
    {
        List<TCell>? _children;
        public TCell? Parent { get; set; }
        public List<TCell>? Children => _children;

        public void AddChild(TCell cell)
        {
            _children = _children ?? new List<TCell>();
            _children.Add(cell);
        }
    }

    public interface IFactoryHexGrid<TGrid>
    {
        TGrid CreateGrid(int columns, int rows);
    }

    public class GridLayerGenerator
    {
        /// <summary>
        /// The exact children of parent cells of the first and last row also lie in these rows
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="factory"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        static public TGrid ExpandBroad<TGrid, TCell>(TGrid grid, IFactoryHexGrid<TGrid> factory, RandomExt random)
            where TGrid : IGrid<TCell>
            where TCell : ILayerCell<TCell>, INeighbors<TCell>
        {
            TGrid childGrid = factory.CreateGrid(grid.Columns * 2, grid.Rows * 2 + 1);

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    TCell parentCell = grid.GetCell(x, y);
                    int childX = x * 2 + y % 2;
                    int childY = y * 2 + 1;
                    TCell childCell = childGrid.GetCell(childX, childY);
                    childCell.Parent = parentCell;
                    parentCell.AddChild(childCell);
                }
            }

            Dictionary<TCell, TCell?> parentByCell = new();
            foreach (TCell cell in childGrid.Cells.Where(c => c.Parent == null))
            {
                List<TCell?> parentCandidates = cell.Neighbors.Select(n => n.Parent).Where(p => p != null).ToList();
                TCell? parent = random.NextItem(parentCandidates);
                parentByCell[cell] = parent;
            }
            foreach (var item in parentByCell)
            {
                item.Key.Parent = item.Value;
            }

            return childGrid;
        }
    }
}
