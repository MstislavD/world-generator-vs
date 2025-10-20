using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Topology
{
    /// <summary>
    /// An interface of a graph node.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public interface INode<TNode>
    {
        IEnumerable<TNode> Neighbors { get; }
    }

    /// <summary>
    /// An interface of a graph node with edge references.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    /// <typeparam name="TEdge"></typeparam>
    public interface INode<TNode, TEdge>
    {
        public TEdge GetEdgeByNeighbor(TNode neighbor);
    }

    public class Node
    {
        /// <summary>
        /// Returns graph nodes connected to the starting one and satisfying the condition
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <param name="start"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static IEnumerable<TNode> Flood<TNode>(TNode start, Func<TNode, bool> condition)
            where TNode : INode<TNode>
        {
            HashSet<TNode> flooded = [];
            HashSet<TNode> flooding = [start];

            while (flooding.Count > 0)
            {
                flooded.UnionWith(flooding);
                flooding = flooding.SelectMany(c => c.Neighbors).Where(condition).Except(flooded).ToHashSet();
            }

            return flooded;
        }

        /// <summary>
        /// Returns true if the graph node is a bottle-neck connection for its neighbors based on
        /// a given property.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="node">The node tested.</param>
        /// <param name="value_func">The function that returns a node's property.</param>
        /// <returns></returns>
        public static bool IsConnection<TNode, T>(TNode node, Func<TNode, T> value_func)
            where TNode : INode<TNode>
            where T : notnull
        {
            HashSet<TNode> sameNeighbors = node.Neighbors.Where(c => value_func(c).Equals(value_func(node))).ToHashSet();
            if (sameNeighbors.Count == 0)
            {
                return false;
            }
            TNode starterCell = sameNeighbors.First();
            IEnumerable<TNode> connectedNeighbors = Flood(starterCell, sameNeighbors.Contains);
            return sameNeighbors.Count > connectedNeighbors.Count();
        }
    }
}
