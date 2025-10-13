using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace WorldSimulationForm
{
    internal class RaycastTest
    {

        internal RaycastTest()
        {

        }

        internal Bitmap GetImage(int size)
        {
            HexCell hex = new HexCell();
            double outerR = size * 0.4;
            double innerR = outerR * Math.Sqrt(3);

            hex.Center = 0.5 * new Vector2(size, size);

            Vector2[] vertices = [
                new Vector2(hex.Center.X, hex.Center.Y - outerR),
                new Vector2(hex.Center.X + 0.5 * innerR, hex.Center.Y - outerR / 2),
                new Vector2(hex.Center.X + 0.5 * innerR, hex.Center.Y + outerR / 2),
                new Vector2(hex.Center.X, hex.Center.Y + outerR),
                new Vector2(hex.Center.X - 0.5 * innerR, hex.Center.Y + outerR / 2),
                new Vector2(hex.Center.X - 0.5 * innerR, hex.Center.Y - outerR / 2)
                ];

            for (int i = 0; i < 6; i++)
            {
                hex.SetVertex(vertices[i], i);
            }

            throw new NotImplementedException();
        }
    }
}
