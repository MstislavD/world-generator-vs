using Topology;
using Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PointLocation
{

    public class PointLocator<TRegion>
    {
        Node _root;
        Dictionary<Edge, TRegion> _regionByEdge;
        HashSet<Trapezoid> _trapezoids;

        public PointLocator(IRegionPartition<TRegion> partition) : this(partition, new RandomExt()) { }

        public PointLocator(IRegionPartition<TRegion> partition, RandomExt random)
        {
            _regionByEdge = new Dictionary<Edge, TRegion>();
            _trapezoids = new HashSet<Trapezoid>();
            _root = new Node();
            LastTrapezoid = null;

            List<Edge> edges = _findAllEdges(partition);

            while (edges.Count > 0)
            {
                Edge edge = random.NextItemExtract(edges);
                if (edge.Vertex1.X != edge.Vertex2.X)
                {
                    _insertEdge(edge, partition.Epsilon);
                }
            }
        }

        public PointLocator(IRegionPartition<TRegion> partition, RandomExt random, int count)
        {
            List<Edge> edges = _findAllEdges(partition);

            for (int i = 0; i < count && edges.Count > 0; i++)
            {
                Edge edge = random.NextItemExtract(edges);
                if (edge.Vertex1.X != edge.Vertex2.X)
                {
                    bool result = _insertEdge(edge, partition.Epsilon);
                    if (!result && edges.Count > 0)
                        i -= 1;
                    
                }
                else
                {
                    i -= 1;
                }
            }
        }

        private List<Edge> _findAllEdges(IRegionPartition<TRegion> partition)
        {
            _root.Trapezoid = new Trapezoid();
            _root.Trapezoid.Left = new Vector2(partition.Left, partition.Top);
            _root.Trapezoid.Right = new Vector2(partition.Right, partition.Bottom);
            _root.Trapezoid.Bottom = new Edge() { Vertex1 = new Vector2(partition.Left, partition.Bottom), Vertex2 = _root.Trapezoid.Right };
            _root.Trapezoid.Top = new Edge() { Vertex1 = _root.Trapezoid.Left, Vertex2 = new Vector2(partition.Right, partition.Top) };
            _root.Trapezoid.Node = _root;

            List<Edge> edges = new List<Edge>();
            foreach (TRegion region in partition.Regions)
            {
                foreach (Edge edge in partition.Edges(region))
                {
                    edges.Add(edge);
                    if (edge.Right == edge.Vertex1)
                    {
                        _regionByEdge[edge] = region;
                    }                    
                }
            }

            return edges;
        }

        public IEnumerable<Trapezoid> GetTrapezoids => _trapezoids;

        Trapezoid _getTrapezoid(double x, double y)
        {
            Node node = _root;
            Vector2 vertex = new Vector2(x, y);

            while (node.Trapezoid == null)
            {
                if (node.Vertex != null)
                {
                    if (_compareX(vertex, node.Vertex) < 0)
                    {
                        node = node.Left;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
                else
                {
                    if (_vertexIsAbove(vertex, node.Edge, 0))
                    {
                        node = node.Left;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
            }

            return node.Trapezoid;
        }

        public TRegion GetRegion(double x, double y)
        {
            Node node = _root;
            Vector2 vertex = new Vector2(x, y);

            while (node.Trapezoid == null)
            {
                if (node.Vertex != null)
                {
                    if (_compareX(vertex, node.Vertex) < 0)
                    {
                        node = node.Left;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
                else
                {
                    if (_vertexIsAbove(vertex, node.Edge, 0))
                    {
                        node = node.Left;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
            }

            LastTrapezoid = node.Trapezoid;

            if (node.Trapezoid != null && _regionByEdge.ContainsKey(node.Trapezoid.Top))
            {
                return _regionByEdge[node.Trapezoid.Top];
            }

            return default;
        }

        public Trapezoid? LastTrapezoid { get; private set; }
      

        bool _insertEdge(Edge edge, double epsilon)
        {
            List<Trapezoid> trapezoids = new List<Trapezoid>();

            Node node = _root;

            Vector2 leftVertex = edge.Left;
            Vector2 rightVertex = edge.Right;

            while (node.Trapezoid == null)
            {
                if (node.Vertex != null)
                {
                    //if (_compareX(leftVertex, node.Vertex) < 0)
                    if (leftVertex.X < node.Vertex.X)
                    {
                        node = node.Left;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
                else
                {
                    if (_equals(edge, node.Edge))
                    {
                        if (edge.Right.Equals(edge.Vertex1))
                        {
                            _regionByEdge[node.Edge] = _regionByEdge[edge];
                        }
                        return false;
                    }
                    else
                    {
                        Vector2 vertex = _compareX(leftVertex, node.Edge.Left) == 0 ? rightVertex : leftVertex;

                        if (_vertexIsAbove(vertex, node.Edge, epsilon))
                        {
                            node = node.Left;
                        }
                        else
                        {
                            node = node.Right;
                        }
                    }
                   
                }
            }

            Trapezoid trapezoid = node.Trapezoid;

            while (trapezoid != null && rightVertex.X > trapezoid.Left.X)
            {
                trapezoids.Add(trapezoid);

                //if (trapezoid.UpperRight == null)
                //{
                //    trapezoid = trapezoid.LowerRight;
                //}
                //else if (trapezoid.LowerRight == null)
                //{
                //    trapezoid = trapezoid.UpperRight;
                //}
                //else
                //{
                //    trapezoid = _vertexIsAbove(trapezoid.UpperRight.Bottom.Left, edge, epsilon) ? trapezoid.LowerRight : trapezoid.UpperRight;
                //}

                if (trapezoid.UpperRight != null && !_vertexIsAbove(trapezoid.UpperRight.Bottom.Left, edge, epsilon))
                    trapezoid = trapezoid.UpperRight;
                else if (trapezoid.LowerRight != null && _vertexIsAbove(trapezoid.LowerRight.Top.Left, edge, epsilon))
                    trapezoid = trapezoid.LowerRight;
                else if (rightVertex.X > trapezoid.Right.X)
                {
                    Vector2 intersection = edge.GetIntersectionByX(trapezoid.Right.X);
                    trapezoid = _getTrapezoid(trapezoid.Right.X + 0.000001, intersection.Y);
                }
                else
                    trapezoid = null;
                    
            }

            _partitionTrapezoids(trapezoids, edge);

            return trapezoids.Count > 0;
        }

        private List<Trapezoid> _partitionTrapezoid(Trapezoid tr0, Edge edge, List<Trapezoid> previousTrapezoids = null)
        {
            Vector2 leftVertex = edge.Vertex1.X < edge.Vertex2.X ? edge.Vertex1 : edge.Vertex2;
            Vector2 rightVertex = edge.Vertex1.X < edge.Vertex2.X ? edge.Vertex2 : edge.Vertex1;

            Trapezoid trA = new Trapezoid();
            Trapezoid trB = new Trapezoid();
            Trapezoid trC = new Trapezoid();
            Trapezoid trD = new Trapezoid();       

            Node nodeA = new Node();
            Node nodeB = new Node() { Trapezoid = trB };
            Node nodeC = new Node() { Trapezoid = trC };
            Node nodeD = new Node();
            Node nodeS = new Node() { Edge = edge, Left = nodeB, Right = nodeC };
            Node nodeQ = new Node() { Vertex = rightVertex, Left = nodeS, Right = nodeD };

            Node node = tr0.Node;
            bool nodeContraction = false;

            node.Trapezoid = null;
            if (leftVertex.X > tr0.Left.X)
            {
                node.Vertex = leftVertex;
                node.Left = nodeA;
                node.Right = rightVertex.X < tr0.Right.X ? nodeQ : nodeS;
            }
            else if (rightVertex.X < tr0.Right.X)
            {
                node.Vertex = rightVertex;
                node.Right = nodeD;
                node.Left = nodeS;
            }
            else
            {
                nodeContraction = true;
                node.Edge = edge;
                node.Left = nodeB;
                node.Right = nodeC;
            }

            if (leftVertex.X > tr0.Left.X)
            {
                nodeA.Trapezoid = trA;
                trA.Node = nodeA;
                trA.Left = tr0.Left;
                trA.Right = leftVertex;
                trA.Top = tr0.Top;
                trA.Bottom = tr0.Bottom;
                trA.UpperLeft = tr0.UpperLeft;
                if (tr0.UpperLeft != null)
                {
                    tr0.UpperLeft.UpperRight = trA;
                }
                trA.LowerLeft = tr0.LowerLeft;
                if (tr0.LowerLeft != null)
                {
                    tr0.LowerLeft.LowerRight = trA;
                }
                trA.UpperRight = trB;
                trA.LowerRight = trC;
                _trapezoids.Add(trA);
            }

            if (rightVertex.X < tr0.Right.X)
            {
                nodeD.Trapezoid = trD;
                trD.Node = nodeD;
                trD.Left = rightVertex;
                trD.Right = tr0.Right;
                trD.Top = tr0.Top;
                trD.Bottom = tr0.Bottom;
                trD.UpperLeft = trB;
                trD.LowerLeft = trC;
                trD.UpperRight = tr0.UpperRight;
                if (tr0.UpperRight != null)
                {
                    tr0.UpperRight.UpperLeft = trD;
                }
                trD.LowerRight = tr0.LowerRight;
                if (tr0.LowerRight != null)
                {
                    tr0.LowerRight.LowerLeft = trD;
                }
                _trapezoids.Add(trD);
            }

            trB.Node = nodeB;
            trB.Left = leftVertex.X > tr0.Left.X ? leftVertex : tr0.Left;
            trB.Right = rightVertex.X < tr0.Right.X ? rightVertex : tr0.Right;
            trB.Top = tr0.Top;
            trB.Bottom = edge;
            if (leftVertex.X > tr0.Left.X)
            {
                trB.UpperLeft = trA;
            }
            else if (tr0.UpperLeft != null)
            {
                trB.UpperLeft = tr0.UpperLeft;
                tr0.UpperLeft.UpperRight = trB;
            }           
            if (rightVertex.X < tr0.Right.X)
            {
                trB.UpperRight = trD;
            }
            else if (tr0.UpperRight != null)
            {
                trB.UpperRight = tr0.UpperRight;
                tr0.UpperRight.UpperLeft = trB;
            }
            trB.LowerLeft = null;
            trB.LowerRight = null;
            _trapezoids.Add(trB);

            trC.Node = nodeC;
            trC.Left = leftVertex.X > tr0.Left.X ? leftVertex : tr0.Left;
            trC.Right = rightVertex.X < tr0.Right.X ? rightVertex : tr0.Right;
            trC.Top = edge;
            trC.Bottom = tr0.Bottom;
            if (leftVertex.X > tr0.Left.X)
            {
                trC.LowerLeft = trA;
            }
            else if (tr0.LowerLeft != null)
            {
                trC.LowerLeft = tr0.LowerLeft;
                tr0.LowerLeft.LowerRight = trC;
            }
            if (rightVertex.X < tr0.Right.X)
            {
                trC.LowerRight = trD;
            }
            else if (tr0.LowerRight != null)
            {
                trC.LowerRight = tr0.LowerRight;
                tr0.LowerRight.LowerLeft = trC;
            }
            trC.UpperLeft = null;
            trC.UpperRight = null;
            _trapezoids.Add(trC);
    

            if (previousTrapezoids!= null)
            {
                if (previousTrapezoids[0].Top.Equals(trB.Top))
                {
                    Trapezoid trX = previousTrapezoids[0];

                    trX.Right = trB.Right;
                    nodeS.Left = trX.Node;

                    if (nodeContraction)
                    {
                        node.Left = trX.Node;
                    }
                    nodeS.Left = trX.Node;

                    trX.UpperRight = trB.UpperRight;
                    if (trB.UpperRight != null)
                    {                        
                        trB.UpperRight.UpperLeft = trX;
                    }


                    _trapezoids.Remove(trB);
                    trB = trX;
                    
                }
                else if (previousTrapezoids[0].Bottom.Equals(trB.Bottom))
                {
                    Trapezoid trX = previousTrapezoids[0];
                    trB.LowerLeft = trX;
                    trX.LowerRight = trB;
                }

                if (previousTrapezoids[1].Bottom.Equals(trC.Bottom))
                {
                    Trapezoid trX = previousTrapezoids[1];

                    trX.Right = trC.Right;

                    if (nodeContraction)
                    {
                        node.Right = trX.Node;
                    }
                    nodeS.Right = trX.Node;                

                    trX.LowerRight = trC.LowerRight;
                    if (trC.LowerRight != null)                    {
                        
                        trC.LowerRight.LowerLeft = trX;
                    }

                    _trapezoids.Remove(trC);
                    trC = trX;
                }
                else if (previousTrapezoids[1].Top.Equals(trC.Top))
                {
                    Trapezoid trX = previousTrapezoids[1];
                    trC.UpperLeft = trX;
                    trX.UpperRight = trC;
                }
            }

            _trapezoids.Remove(tr0);

            return new List<Trapezoid>() { trB, trC };
        }

        private void _partitionTrapezoids(List<Trapezoid> trapezoids, Edge edge)
        {
            List<Trapezoid> lastPartition = _partitionTrapezoid(trapezoids[0], edge);

            if (trapezoids.Count > 0)
            {
                for (int i = 1; i < trapezoids.Count; i++)
                {
                    lastPartition = _partitionTrapezoid(trapezoids[i], edge, lastPartition);
                }
            }
        }

        bool _vertexIsAbove(Vector2 v, Edge e, double epsilon)
        {
            double x1 = v.X - e.Left.X;
            double y1 = v.Y - e.Left.Y;
            double x2 = e.Right.X - e.Left.X;
            double y2 = e.Right.Y - e.Left.Y;

            return x1 * y2 - y1 * x2 < 0;
        }

        int _compareX(Vector2 v1, Vector2 v2)
        {
            if (v1.X < v2.X)
            {
                return -1;
            }
            else if (v1.X > v2.X)
            {
                return 1;
            }
            else if (v1.Y < v2.Y)
            {
                return -1;
            }
            else if (v1.Y > v2.Y)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        bool _equals(Edge e1, Edge e2)
        {
            return e1.Right.X == e2.Right.X && e1.Right.Y == e2.Right.Y && e1.Left.X == e2.Left.X && e1.Left.Y == e2.Left.Y;
        } 
    }
}
