using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexGrid;

namespace PointLocationForm
{
    class Region
    {
        List<Edge> _edges;

        public Region(IEnumerable<Vertex> vertices)
        {
            _edges = new List<Edge>();

            Vertex prevVertex = null;
            foreach(Vertex v in vertices)
            {
                if (prevVertex != null)
                {
                    _createEdge(prevVertex, v);
                }
                prevVertex = v;
            }

            _createEdge(prevVertex, vertices.First());
        }

        public IEnumerable<Edge> Edges => _edges;

        public IEnumerable<Vertex> Vertices => _edges.Select(e => e.Vertex1);

        private void _createEdge(Vertex v1, Vertex v2)
        {
            Edge edge = new Edge();
            edge.Vertex1 = v1;
            edge.Vertex2 = v2;
            _edges.Add(edge);
        }
    }
}
