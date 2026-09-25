using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    class Node<T>
    {
        public T Item { get; set; }
        public Node<T>? Left { get; set; }
        public Node<T>? Right { get; set; }
        public Node<T>? Parent { get; set; }
        public double TotalWeight { get; set; }
        public double ItemWeight { get; set; }
        public int SubtreeCount { get; set; }
        public Node(T item) { Item = item; }
        public override string ToString()
        {
            return $"Node: {Item}, Weight: {ItemWeight}, Total weight: {TotalWeight}";
        }
    }

    public class WeightedTree<T>
        where T : notnull
    {
        Dictionary<T, Node<T>> _nodeByItem = new Dictionary<T, Node<T>>();
        Node<T>? _root;

        public void Add(T item, double weight)
        {
            if (!_nodeByItem.ContainsKey(item))
            {
                Node<T> node = _add(_root, null, item, weight);
                _nodeByItem[item] = node;
                _root = _root ?? node;
            }
            else
            {
                Node<T>? node = _nodeByItem[item];
                double weightDelta = weight - node.ItemWeight;
                node.ItemWeight = weight;
                while (node != null)
                {
                    node.TotalWeight += weightDelta;
                    node = node.Parent;
                }
            }
        }

        /// <summary>
        /// Removes the item from the tree and rebalances the weight sums.
        /// Returns false if the item was not present in the tree.
        /// </summary>
        public bool Remove(T item)
        {
            if (!_nodeByItem.TryGetValue(item, out Node<T>? node))
            {
                return false;
            }
            _nodeByItem.Remove(item);

            // The tree keeps no ordering invariant, so the removed node's slot
            // can be taken over by any of its subtrees. `top` is the subtree that
            // takes the slot; in the leaf case it is the removed node itself,
            // whose parent link is still intact after unlinking.
            Node<T>? top = node;
            if (node.Left == null && node.Right == null)
            {
                _unlink(node);
            }
            else if (node.Left != null && node.Right != null)
            {
                Node<T> left = node.Left;
                Node<T> right = node.Right;

                // Reattach the right subtree as a leaf of the left subtree.
                Node<T> slot = left;
                while (slot.Left != null && slot.Right != null)
                {
                    slot = slot.Left.SubtreeCount > slot.Right.SubtreeCount ? slot.Left : slot.Right;
                }
                if (slot.Left == null)
                {
                    slot.Left = right;
                }
                else
                {
                    slot.Right = right;
                }
                right.Parent = slot;
                for (Node<T>? p = slot; p != null && p != node; p = p.Parent)
                {
                    p.TotalWeight += right.TotalWeight;
                    p.SubtreeCount += right.SubtreeCount;
                }

                _promote(left, node);
                top = left;
            }
            else
            {
                Node<T> child = node.Left ?? node.Right!;
                _promote(child, node);
                top = child;
            }

            // Drop the removed item's weight and count from every ancestor.
            for (Node<T>? p = top?.Parent; p != null; p = p.Parent)
            {
                p.TotalWeight -= node.ItemWeight;
                p.SubtreeCount -= 1;
            }

            return true;
        }

        public T Extract(Random random)
        {
            return Extract(random.NextDouble());
        }

        public T Extract(double randomDouble)
        {
            if (_root == null)
                throw new Exception("WeightedTree is empty.");

            if (randomDouble < 0 || randomDouble > 1)
            {
                throw new ArgumentOutOfRangeException("Random double parameter must be in [0,1] interval.");
            }
            double value = _root.TotalWeight * randomDouble;
            Node<T> extracted = _extract(_root, value);
            _nodeByItem.Remove(extracted.Item);
            return extracted.Item;
        }

        public IEnumerable<string> Walk
        {
            get
            {
                List<Node<T>> list = new List<Node<T>>();
                if (_root != null)
                {
                    _walk(_root, list);
                }
                return list.Select(n => n.ToString());
            }
        }

        public int Count => _root == null ? 0 : _root.SubtreeCount;
        public double Weight => _root == null ? 0 : _root.TotalWeight;
        public bool Contains(T item) => _nodeByItem.ContainsKey(item);
        public double GetWeight(T item) => _nodeByItem[item].ItemWeight;

        void _unlink(Node<T> node)
        {
            if (node.Parent == null)
            {
                _root = null;
            }
            else if (node.Parent.Left == node)
            {
                node.Parent.Left = null;
            }
            else
            {
                node.Parent.Right = null;
            }
        }

        void _promote(Node<T> child, Node<T> removed)
        {
            if (removed.Parent == null)
            {
                _root = child;
            }
            else if (removed.Parent.Left == removed)
            {
                removed.Parent.Left = child;
            }
            else
            {
                removed.Parent.Right = child;
            }
            child.Parent = removed.Parent;
        }

        Node<T> _add(Node<T>? node, Node<T>? parentNode, T item, double weight)
        {
            Node<T> newNode;
            if (node == null)
            {
                newNode = new Node<T>(item);
                newNode.ItemWeight = weight;
                newNode.TotalWeight = weight;
                newNode.Parent = parentNode;
                newNode.SubtreeCount += 1;
            }
            else
            {
                node.TotalWeight += weight;
                node.SubtreeCount += 1;
                if (node.Left == null)
                {
                    newNode = _add(null, node, item, weight);
                    node.Left = newNode;
                }
                else if(node.Right == null)
                {
                    newNode = _add(null, node, item, weight);
                    node.Right = newNode;
                    
                }
                else if(node.Left.SubtreeCount > node.Right.SubtreeCount)
                {
                    newNode = _add(node.Right, node, item, weight);
                }
                else
                {
                    newNode = _add(node.Left, node, item, weight);
                }
            }
            return newNode;
        }

      
        Node<T> _extract(Node<T> node, double value)
        {

            double value1 = node.Left == null ? 0 : node.Left.TotalWeight;
            double value2 = value1 + node.ItemWeight;            

            if (value < value1)
            {
                Node<T> extracted = _extract(node.Left, value);
                node.TotalWeight -= extracted.ItemWeight;
                node.SubtreeCount -= 1;
                return extracted;
            }
            else if (value <= value2)
            {
                int subtreeCount = node.SubtreeCount;
                Node<T> substitute = _substitute(node);

                substitute.Left = node.Left;
                substitute.Right = node.Right;
                substitute.SubtreeCount = subtreeCount - 1;
                substitute.TotalWeight = node.TotalWeight - node.ItemWeight + substitute.ItemWeight;
                substitute.Parent = node.Parent;

                if (!node.Equals(substitute))
                {
                    if (node.Equals(_root))
                    {
                        _root = substitute;
                    }
                    if (node.Left != null)
                    {
                        node.Left.Parent = substitute;
                    }
                    if (node.Right!= null)
                    {
                        node.Right.Parent = substitute;
                    }
                    if (node.Parent != null)
                    {
                        if (node.Equals(node.Parent.Left))
                        {
                            node.Parent.Left = substitute;
                        }
                        else
                        {
                            node.Parent.Right = substitute;
                        }
                    }
                }
                else
                {
                    if (node.Parent == null)
                    {
                        _root = null;
                    }
                    else
                    {
                        if (node.Equals(node.Parent.Left))
                        {
                            node.Parent.Left = null;
                        }
                        else
                        {
                            node.Parent.Right = null;
                        }
                    }
                }

                return node;
            }
            else
            {
                Node<T> extracted = _extract(node.Right, value - value2);
                node.TotalWeight -= extracted.ItemWeight;
                node.SubtreeCount -= 1;
                return extracted;
            }
        }

        void _walk(Node<T> root, List<Node<T>> list)
        {
            list.Add(root);
            if (root.Left != null)
            {
                _walk(root.Left, list);
            }            
            if (root.Right != null)
            {
                _walk(root.Right, list);
            }
        }

        Node<T> _substitute(Node<T> node)
        {
            Node<T> sNode;

            if (node.Left == null && node.Right == null)
            {
                sNode = node;
            }
            else if (node.Left == null || (node.Right != null && node.Right.SubtreeCount < node.Left.SubtreeCount))
            {
                sNode = _substitute(node.Right);
                if (sNode.Equals(node.Right))
                {
                    node.Right = null;
                }
                node.SubtreeCount -= 1;
                node.TotalWeight -= sNode.ItemWeight;
            }
            else
            {
                sNode = _substitute(node.Left);
                if (sNode.Equals(node.Left))
                {
                    node.Left = null;
                }
                node.SubtreeCount -= 1;
                node.TotalWeight -= sNode.ItemWeight;
            }

            return sNode;
        }        
    }
}
