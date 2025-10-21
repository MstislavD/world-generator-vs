using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace Topology
{
    public interface ITreeNode<T>
    {
        T? Parent { get; set; }
        IEnumerable<T>? Children { get; }
        int ChildrenCount { get; }
        void AddChild(T cell);
    }

    public class TreeNode
    {
        public static T GetRoot<T>(T node)
            where T: ITreeNode<T>
        {
            if (node.Parent == null)
                return node;
            else
                return GetRoot(node.Parent);
        }
    }

    public class LayerHexCell<TCell, TEdge> : HexCell<TCell, TEdge>, ITreeNode<TCell>
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

    public class LayerEdge<TCell, TEdge> : Edge<TCell>, ITreeNode<TEdge>
    {
        List<TEdge>? _children;
        public TEdge? Parent { get; set; }
        public IEnumerable<TEdge>? Children => _children;
        public int ChildrenCount => _children == null ? 0 : _children.Count;
        public void AddChild(TEdge cell)
        {
            _children = _children ?? new List<TEdge>();
            _children.Add(cell);
        }
    }

    public interface IFactoryGrid<TGrid>
    {
        TGrid CreateGrid(int columns, int rows);
    }

    public class ChildGridGenerator
    {
        /// <summary>
        /// Generates child grid from a grid.
        /// </summary>
        /// <param name="parentGrid">Parent grid.</param>
        /// <param name="factory"></param>
        /// <param name="random"></param>
        /// <param name="wideBorders">False if exact children of parent cells of the first and last row also lie in these rows.</param>
        /// <returns></returns>
        static public TGrid CreateChildGrid<TGrid, TCell, TEdge>(TGrid parentGrid, IFactoryGrid<TGrid> factory,
            RandomExt random, bool wideBorders = false, int? sizeVariance = null)
            where TGrid : IHexGrid, IGrid<TCell>, IEdges<TEdge>
            where TCell : INode<TCell>, INode<TCell, TEdge>, ITreeNode<TCell>
            where TEdge : IEdge<TCell>, ITreeNode<TEdge>
        {
            int dh = wideBorders ? 1 : -1;
            TGrid childGrid = factory.CreateGrid(parentGrid.Columns * 2, parentGrid.Rows * 2 + dh);

            Dictionary<TCell, TCell> parentTmp = new Dictionary<TCell, TCell>();
            Dictionary<TCell, int> sizeByParent = new Dictionary<TCell, int>();

            int dy = wideBorders ? 1 : 0;
            for (int x = 0; x < parentGrid.Columns; x++)
            {
                for (int y = 0; y < parentGrid.Rows; y++)
                {
                    TCell parentCell = parentGrid.GetCell(x, y);
                    int childX = x * 2 + y % 2;
                    int childY = y * 2 + dy;
                    TCell childCell = childGrid.GetCell(childX, childY);
                    parentTmp[childCell] = parentCell;
                    sizeByParent[parentCell] = 1;
                    childCell.Parent = parentCell;
                    parentCell.AddChild(childCell);
                }
            }

            IEnumerable<TCell> cells = random.Permutation(childGrid.Cells.Where(c => !parentTmp.ContainsKey(c)).ToList());

            foreach (TCell cell in cells)
            {
                List<TCell> candidates = cell.Neighbors.Where(parentTmp.ContainsKey).Select(c => parentTmp[c]).ToList();

                TCell? parent = default;
                if (candidates.Count > 1)
                {
                    if (sizeVariance != null)
                    {
                        int sizeDelta = sizeByParent[candidates[0]] - sizeByParent[candidates[1]];
                        if (sizeDelta > sizeVariance)
                        {
                            parent = candidates[1];
                        }
                        else if (sizeDelta < -sizeVariance)
                        {
                            parent = candidates[0];
                        }
                        else
                        {
                            parent = random.NextItem(candidates);
                        }
                    }
                    else
                    {
                        parent = random.NextItem(candidates);
                    }
                }
                else
                {
                    parent = candidates[0];
                }

                sizeByParent[parent] += 1;
                cell.Parent = parent ?? throw new Exception();
                parent.AddChild(cell);
            }

            foreach (TEdge edge in childGrid.Edges)
            {
                if (edge.Cell1 != null && edge.Cell2 != null)
                {
                    TCell? p1 = edge.Cell1.Parent;
                    TCell? p2 = edge.Cell2.Parent;
                    if (p1 != null && !p1.Equals(p2))
                    {
                        TEdge parent = p1.GetEdgeByNeighbor(p2);
                        edge.Parent = parent;
                    }
                }
            }

            return childGrid;
        }
    }
}
