using Topology;
using System.Drawing;

namespace WorldSimulationForm
{
    struct ImageData
    {
        public Vector2 Vertex;
        public Image Image;
        public bool Stretch;
        public float Scale;

        public ImageData(Vector2 vertex, Image image) : this(vertex, image, 1.0f) { }
        public ImageData(Vector2 vertex, Image image, float scale)
        {
            Vertex = vertex;
            Image = image;
            Stretch = false;
            Scale = scale;
        }
    }
}
