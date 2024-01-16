using HexGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSim
{
    public enum Belt { Polar, Boreal, Temperate, Subtropical, Tropical, NA }
    public enum Humidity { Dry, Seasonal, Wet, NA }
    public class CellData
    {
        public HexCell Parent { get; set; }
        public Elevation Elevation { get; set; } = Elevation.DeepOcean;
        public int Height { get; set; }
    }
}
