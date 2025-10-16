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
        TCell? Parent { get; set; }
        IEnumerable<TCell>? Children { get; }
        int ChildrenCount { get; }
        void AddChild(TCell cell);
    }

    public class LayerHexCell<TCell, TEdge> : HexCell<TCell, TEdge>, ILayerCell<TCell>
    {
        List<TCell>? _children;
        public TCell? Parent { get; set; }
        public IEnumerable<TCell>? Children => _children;
        public int ChildrenCount => _children == null ? 0 : _children.Count;
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
        /// Generates child grid from a grid.
        /// </summary>
        /// <param name="parentGrid">Parent grid.</param>
        /// <param name="factory"></param>
        /// <param name="random"></param>
        /// <param name="wideBorders">False if exact children of parent cells of the first and last row also lie in these rows.</param>
        /// <returns></returns>
        static public TGrid CreateLayerGrid<TGrid, TCell>(TGrid parentGrid, IFactoryHexGrid<TGrid> factory, RandomExt random, bool wideBorders = false)
            where TGrid : IGrid<TCell>
            where TCell : ILayerCell<TCell>, INeighbors<TCell>
        {
            int dh = wideBorders ? 1 : -1;
            TGrid childGrid = factory.CreateGrid(parentGrid.Columns * 2, parentGrid.Rows * 2 + dh);

            int dy = wideBorders ? 1 : 0;
            for (int x = 0; x < parentGrid.Columns; x++)
            {
                for (int y = 0; y < parentGrid.Rows; y++)
                {
                    TCell parentCell = parentGrid.GetCell(x, y);
                    int childX = x * 2 + y % 2;
                    int childY = y * 2 + dy;
                    TCell childCell = childGrid.GetCell(childX, childY);
                    childCell.Parent = parentCell;
                    parentCell.AddChild(childCell);
                }
            }

            int sizeVariance = 0; // dummy
            foreach (TCell cell in childGrid.Cells.Where(c => c.Parent == null))
            {
                List<TCell> parentCandidates = cell.Neighbors.Select(n => n.Parent).Where(p => p != null).Distinct().ToList();

                TCell? parent = default;
                if (parentCandidates.Count > 1)
                {                    
                    if (sizeVariance > 0) // dummy check
                    {
                        int sizeDelta = parentCandidates[0].ChildrenCount - parentCandidates[1].ChildrenCount;
                        if (sizeDelta > sizeVariance)
                        {
                            parent = parentCandidates[1];
                        }
                        else if (sizeDelta < -sizeVariance)
                        {
                            parent = parentCandidates[0];
                        }
                        else
                        {
                            parent = random.NextItem(parentCandidates);
                        }
                    }
                }
                else
                {
                    parent = parentCandidates[0];
                }

                cell.Parent = parent ?? throw new Exception();
                parent.AddChild(cell);
            }

            return childGrid;
        }
    }
}
