using System.Collections.Generic;
using System.Linq;
using Topology;

namespace WorldSimulation
{
    public class SubregionS<TSubregion, TCell, TEdge> : IPolygon, INode<TSubregion>, IEdges<LineSegment>
        where TSubregion: SubregionS<TSubregion, TCell, TEdge>
        where TEdge : LineSegment, new()
    {
        BoundingBox? _bbox = null;
        List<SubregionEdge> _edges = new List<SubregionEdge>();
        Dictionary<SubregionEdge, TSubregion> _neighborByEdge = new Dictionary<SubregionEdge, TSubregion>();

        public void AddEdge(SubregionEdge sEdge) => _edges.Add(sEdge);

        public SubregionType Type { get; set; }
        public TEdge Edge { get; set; }
        public TCell Cell { get; set; }
        public Region Region { get; internal set; }
        public TEdge ParentEdge { get; set; }
        public TCell ParentCell { get; set; }
        public Vector2 Center { get; set; }
        public void AddNeighbor(TSubregion neighbor, SubregionEdge sEdge) => _neighborByEdge[sEdge] = neighbor;
        public IEnumerable<Vector2> Vertices => _edges.SelectMany(e => e.Vertices).Distinct();
        public int VertexCount => Vertices.Count();
        public IEnumerable<SubregionEdge> Edges => _edges;
        public TSubregion GetNeighbor(SubregionEdge edge) => _neighborByEdge[edge];
        public bool HasNeighbor(SubregionEdge edge) => GetNeighbor(edge) != null;
        public SubregionEdge GetEdge(TSubregion neighbor) => _edges.First(e => GetNeighbor(e) == neighbor);
        public IEnumerable<TSubregion> Neighbors => _neighborByEdge.Values.Where(n => n != null);
        public TSubregion Drainage { get; internal set; }
        public bool River { get; internal set; }
        public BoundingBox Bounds => _bbox ?? (_bbox = Polygon.CalculateBoundingBox(this));

        public bool SameRegion(TSubregion subregion)
        {
            if (Type != subregion.Type)
                return false;
            else if (Type == SubregionType.Cell && ParentCell.Equals(subregion.ParentCell))
                return true;
            else if (Type == SubregionType.Edge && ParentEdge.Equals(subregion.ParentEdge))
                return true;
            else
                return false;
        }

        IEnumerable<LineSegment> IEdges<LineSegment>.Edges
        {
            get
            {
                List<Vector2> vertices = Vertices.ToList();
                for (int i = 1; i < vertices.Count; i++)
                {
                    yield return _rounded(new Edge() { Vertex1 = vertices[i - 1], Vertex2 = vertices[i] });
                }
                yield return _rounded(new Edge() { Vertex1 = vertices[vertices.Count - 1], Vertex2 = vertices[0] });
            }
        }

        TEdge _rounded(Edge edge)
        {
            int precision = 5;
            double x1 = Math.Round(edge.Vertex1.X, precision);
            double y1 = Math.Round(edge.Vertex1.Y, precision);
            double x2 = Math.Round(edge.Vertex2.X, precision);
            double y2 = Math.Round(edge.Vertex2.Y, precision);
            return new TEdge() { Vertex1 = new Vector2(x1, y1), Vertex2 = new Vector2(x2, y2) };
        }
    }
}
