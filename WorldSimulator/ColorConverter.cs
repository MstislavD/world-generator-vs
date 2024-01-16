using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WorldSimulator
{
    class ColorConverter : VectorConverter<Color>
    {
        public static VectorConverter<Color> Converter => new ColorConverter();
        public override Color ObjectFromVector(double[] vector) => Color.FromArgb((int)vector[0], (int)vector[1], (int)vector[2]);
        public override double[] ObjectToVector(Color item) => new double[] { item.R, item.G, item.B };

    }
}
