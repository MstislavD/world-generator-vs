using System.Collections.Generic;
using System.Linq;
using Topology;

namespace WorldSimulation
{
    public class Subregion : PolygonBase, INeighbors<Subregion>
    {
        List<SubregionEdge> _edges = new List<SubregionEdge>();
        Dictionary<SubregionEdge, Subregion> _neighborByEdge = new Dictionary<SubregionEdge, Subregion>();

        public void AddEdge(SubregionEdge sEdge) => _edges.Add(sEdge);

        public SubregionType Type { get; set; }
        public Edge Edge { get; set; }
        public HexCell Cell { get; set; }
        public Region Region { get; internal set; }
        public Edge ParentEdge { get; set; }
        public HexCell ParentCell { get; set; }
        public Vector2 Center { get; set; }
        public void AddNeighbor(Subregion neighbor, SubregionEdge sEdge) => _neighborByEdge[sEdge] = neighbor;
        public override IEnumerable<Vector2> Vertices => _edges.SelectMany(e => e.Vertices).Distinct();

        public override int VertexCount => Vertices.Count();
        public IEnumerable<SubregionEdge> Edges => _edges;
        public Subregion GetNeighbor(SubregionEdge edge) => _neighborByEdge[edge];
        public bool HasNeighbor(SubregionEdge edge) => GetNeighbor(edge) != null;
        public SubregionEdge GetEdge(Subregion neighbor) => _edges.First(e => GetNeighbor(e) == neighbor);
        public IEnumerable<Subregion> Neighbors => _neighborByEdge.Values.Where(n => n != null);
        public Subregion Drainage { get; internal set; }
        public bool River { get; internal set; }
        public bool SameRegion(Subregion subregion)
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
    }
}
