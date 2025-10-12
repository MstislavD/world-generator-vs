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

        public T Extract(Random random)
        {
            return Extract(random.NextDouble());
        }

        public T Extract(double randomDouble)
        {
            if (_root == null)
                throw new Exception();

            if (randomDouble<0 || randomDouble > 1)
            {
                throw new Exception("Random double parameter must be in [0,1] interval.");
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
            else if (value < value2)
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
