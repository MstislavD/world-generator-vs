using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace PointLocationForm
{
    class Region
    {
        List<Edge> _edges;

        public Region(IEnumerable<Vector2> vertices)
        {
            _edges = new List<Edge>();

            Vector2 prevVertex = null;
            foreach(Vector2 v in vertices)
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

        public IEnumerable<Vector2> Vertices => _edges.Select(e => e.Vertex1);

        private void _createEdge(Vector2 v1, Vector2 v2)
        {
            Edge edge = new Edge();
            edge.Vertex1 = v1;
            edge.Vertex2 = v2;
            _edges.Add(edge);
        }
    }
}
