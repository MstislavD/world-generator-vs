using HexGrid;
using System.Drawing;

namespace WorldSimulator
{
    struct ImageData
    {
        public Vertex Vertex;
        public Image Image;
        public bool Stretch;
        public float Scale;

        public ImageData(Vertex vertex, Image image) : this(vertex, image, 1.0f) { }
        public ImageData(Vertex vertex, Image image, float scale)
        {
            Vertex = vertex;
            Image = image;
            Stretch = false;
            Scale = scale;
        }
    }
}
