using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topology;

namespace WorldSimulation
{
    class RegionSmoother
    {
        public static void Smooth(WorldGenerator generator, Grid grid)
        {
            foreach (HexCell cell in grid.Cells)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 v = cell.GetVertex(i * 3);
                    HexCell n1 = cell.GetNeighbor(i == 0 ? 5 : (i * 3 - 1));
                    HexCell n2 = cell.GetNeighbor(i * 3);

                    HexCell smoothed = _smoothedHex(cell, n1, n2, generator.IsLand);

                    if (smoothed == null)
                    {
                        smoothed = _smoothedHex(cell, n1, n2, generator.GetCellParent);
                    }

                    if (smoothed == null)
                    {
                        smoothed = _smoothedHex(cell, n1, n2, generator.HasRidge);
                    }

                    if (smoothed != null)
                    {
                        _moveVertex(grid, v, smoothed.Center);
                        if (_borderCell(grid, cell))
                        {
                            if (i == 0 && cell.GetNeighbor(0) != null)
                            {
                                _moveVertex(grid, cell.GetNeighbor(0).GetVertex(4), smoothed.Center);
                            }
                            if (i == 1 && cell.GetNeighbor(2) != null)
                            {
                                _moveVertex(grid, cell.GetNeighbor(2).GetVertex(5), smoothed.Center);
                            }
                        }

                        if (i == 1 && _borderCell(grid, cell.GetNeighbor(3)))
                        {
                            _moveVertex(grid, cell.GetNeighbor(3).GetVertex(1), smoothed.Center);
                        }
                        if (i == 0 && _borderCell(grid, cell.GetNeighbor(5)))
                        {
                            _moveVertex(grid, cell.GetNeighbor(5).GetVertex(2), smoothed.Center);
                        }
                    }

                }

                if (cell.GetNeighbor(0) == null)
                {
                    cell.GetVertex(1).Y = cell.GetVertex(0).Y;
                    cell.GetVertex(5).Y = cell.GetVertex(0).Y;
                }

                if (cell.GetNeighbor(2) == null)
                {
                    cell.GetVertex(2).Y = cell.GetVertex(3).Y;
                    cell.GetVertex(4).Y = cell.GetVertex(3).Y;
                }
            }
        }

        static void _moveVertex(Grid grid, Vector2 v, Vector2 target)
        {
            double dx = target.X - v.X;
            if (dx < -grid.XDimension / 2)
            {
                dx += grid.XDimension;
            }
            if (dx > grid.XDimension / 2)
            {
                dx -= grid.XDimension;
            }
            v.X = dx * 0.25 + v.X;
            v.Y = (target.Y - v.Y) * 0.25 + v.Y;
        }

        static HexCell _smoothedHex<T>(HexCell c1, HexCell c2, HexCell c3, Func<HexCell, T> value)
        {
            if (c1 == null || c2 == null || c3 == null)
            {
                return null;
            }

            T v1 = c1 == null ? default : value(c1);
            T v2 = c2 == null ? default : value(c2);
            T v3 = c3 == null ? default : value(c3);

            if (v1.Equals(v2))
            {
                return v1.Equals(v3) ? null : c3;
            }
            else if (v1.Equals(v3))
            {
                return c2;
            }
            else
            {
                return v2.Equals(v3) ? c1 : null;
            }
        }

        static HexCell _smoothedHex<T>(HexCell c1, HexCell c2, HexCell c3, Func<Edge, T> value)
        {
            if (c1 == null || c2 == null || c3 == null)
            {
                return null;
            }

            T v1 = c1 == null ? default : value(c1.GetEdgeByNeighbor(c2));
            T v2 = c2 == null ? default : value(c1.GetEdgeByNeighbor(c3));
            T v3 = c3 == null ? default : value(c2.GetEdgeByNeighbor(c3));

            if (v1.Equals(v2))
            {
                return v1.Equals(v3) ? null : c1;
            }
            else if (v1.Equals(v3))
            {
                return c2;
            }
            else
            {
                return v2.Equals(v3) ? c3 : null;
            }
        }

        static bool _borderCell(Grid grid, HexCell cell) => cell != null && cell.Center.X + grid.HexSide / 2 > grid.XDimension;
    }
}
