using Topology;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{ 
    public interface IContainer<TGrid, TCell>
    {
        TGrid Grid { get; }
        TCell GetParent(TCell cell);
    }

    public class ExpandedHexGrid : HexGrid<WorldCell, WorldEdge>, IContainer<HexGrid, HexCell>
    {
        public HexGrid ParentGrid;
        public HexGrid Grid { get; set; }
        public Dictionary<HexCell, HexCell> ParentByCell;
        public Dictionary<HexCell, List<HexCell>> ChildrenByCell;

        public ExpandedHexGrid(int columns, int rows) : base(columns, rows) { }

        public ExpandedHexGrid() : base(0, 0) { }

        public IEnumerable<HexCell> Regions => ParentGrid.Cells;
        public IEnumerable<Edge> ChildrenByEdge(Edge edge) => ChildrenByCell[edge.Cell1].SelectMany(c => c.Edges).Where(e => e.Cells.Any(c => ParentByCell[c] == edge.Cell2));

        public HexCell GetParent(HexCell cell) => ParentByCell[cell];
    }

    public class HexGridExpander
    {
        /// <summary>
        /// The exact children of parent cells of the first and last row, also lie in these rows
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        static public ExpandedHexGrid ExpandBroad(HexGrid grid, RandomExt random)
        {
            HexGrid expandedGrid = new HexGrid(grid.Columns * 2, grid.Rows * 2 + 1);
            Dictionary<HexCell, HexCell> parentByCellTmp = new Dictionary<HexCell, HexCell>();
            Dictionary<HexCell, HexCell> parentByCell = new Dictionary<HexCell, HexCell>();
            Dictionary<HexCell, List<HexCell>> childrenByCell = new Dictionary<HexCell, List<HexCell>>();

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    HexCell parentCell = grid.GetCell(x, y);
                    int childX = x * 2 + y % 2;
                    int childY = y * 2 + 1;
                    HexCell childCell = expandedGrid.GetCell(childX, childY);
                    parentByCellTmp[childCell] = parentCell;
                    childrenByCell[parentCell] = new List<HexCell>() { childCell };
                }
            }

            foreach(HexCell cell in expandedGrid.Cells)
            {
                if (parentByCellTmp.ContainsKey(cell))
                {
                    parentByCell[cell] = parentByCellTmp[cell];
                }
                else
                {
                    List<HexCell> parentCandidates = cell.Neighbors.Where(parentByCellTmp.ContainsKey).Select(c => parentByCellTmp[c]).ToList();
                    HexCell parent = random.NextItem(parentCandidates);
                    parentByCell[cell] = parent;
                    childrenByCell[parent].Add(cell);
                }
            }

            return new ExpandedHexGrid() {Grid = expandedGrid, ParentGrid = grid, ParentByCell = parentByCell, ChildrenByCell = childrenByCell };
        }

        /// <summary>
        /// The exact children of parent cells of the first and last row, lie in the second row from the top and from the bottom.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        static public ExpandedHexGrid Expand(HexGrid grid, RandomExt random)
        {
            Topology.HexGrid expandedGrid = new HexGrid(grid.Columns * 2, grid.Rows * 2 - 1);
            Dictionary<HexCell, HexCell> parentByCellTmp = new Dictionary<HexCell, HexCell>();
            Dictionary<HexCell, HexCell> parentByCell = new Dictionary<HexCell, HexCell>();
            Dictionary<HexCell, List<HexCell>> childrenByCell = new Dictionary<HexCell, List<HexCell>>();

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    HexCell parentCell = grid.GetCell(x, y);
                    int childX = x * 2 + y % 2;
                    int childY = y * 2;
                    HexCell childCell = expandedGrid.GetCell(childX, childY);
                    parentByCellTmp[childCell] = parentCell;
                    childrenByCell[parentCell] = new List<HexCell>() { childCell };
                }
            }

            foreach (HexCell cell in expandedGrid.Cells)
            {
                if (parentByCellTmp.ContainsKey(cell))
                {
                    parentByCell[cell] = parentByCellTmp[cell];
                }
                else
                {
                    List<HexCell> parentCandidates = cell.Neighbors.Where(parentByCellTmp.ContainsKey).Select(c => parentByCellTmp[c]).ToList();
                    HexCell parent = random.NextItem(parentCandidates);
                    parentByCell[cell] = parent;
                    childrenByCell[parent].Add(cell);
                }
            }

            return new ExpandedHexGrid() { Grid = expandedGrid, ParentGrid = grid, ParentByCell = parentByCell, ChildrenByCell = childrenByCell };
        }

        /// <summary>
        /// This algoritm makes parent regions more uniform in size.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="random"></param>
        /// <param name="sizeVariance"></param>
        /// <returns></returns>
        static public ExpandedHexGrid Expand(HexGrid grid, RandomExt random, int sizeVariance)
        {
            HexGrid expandedGrid = new HexGrid(grid.Columns * 2, grid.Rows * 2 - 1);
            Dictionary<HexCell, HexCell> parentByCellTmp = new Dictionary<HexCell, HexCell>();
            Dictionary<HexCell, HexCell> parentByCell = new Dictionary<HexCell, HexCell>();
            Dictionary<HexCell, int> sizeByParentCell = new Dictionary<HexCell, int>();
            Dictionary<HexCell, List<HexCell>> childrenByCell = new Dictionary<HexCell, List<HexCell>>();

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    HexCell parentCell = grid.GetCell(x, y);
                    int childX = x * 2 + y % 2;
                    int childY = y * 2;
                    HexCell childCell = expandedGrid.GetCell(childX, childY);
                    parentByCellTmp[childCell] = parentCell;
                    childrenByCell[parentCell] = new List<HexCell>() { childCell };
                    sizeByParentCell[parentCell] = x > 0 && x < grid.Rows - 1 ? 7 : 5;
                }
            }

            parentByCell = new Dictionary<HexCell, HexCell>(parentByCellTmp);

            foreach (HexCell cell in random.Permutation(expandedGrid.Cells.ToList()))
            {
                if (!parentByCell.ContainsKey(cell))
                {
                    List<HexCell> parentCandidates = cell.Neighbors.Where(parentByCellTmp.ContainsKey).Select(c => parentByCellTmp[c]).ToList();
                    int sizeDelta = sizeByParentCell[parentCandidates[0]] - sizeByParentCell[parentCandidates[1]];

                    HexCell parent;
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

                    sizeByParentCell[parentCandidates[0]] -= 1;
                    sizeByParentCell[parentCandidates[1]] -= 1;
                    sizeByParentCell[parent] += 1;

                    parentByCell[cell] = parent;
                    childrenByCell[parent].Add(cell);
                }
            }

            return new ExpandedHexGrid() { Grid = expandedGrid, ParentGrid = grid, ParentByCell = parentByCell, ChildrenByCell = childrenByCell };
        }
    }
}
