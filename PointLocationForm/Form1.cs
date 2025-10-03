using Topology;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RandomExtension;
using PointLocation;
using WorldSimulation;

namespace PointLocationForm
{
    public partial class Form1 : Form
    {
        int _imageLeft;
        int _seed;
        int _step;
        List<Region> _regions;
        HexGrid _grid;
        RegionPartition _partition;
        PointLocator<Region> _locator;
        Dictionary<Region, SolidBrush> _colorByRegion;
        bool _highlight;
        bool _initiated;
        Region _highlightedRegion;

        float _geometrySize = 7.0f;
        int _imageSize = 800;
        int _margin = 5;

        SubregionGraph _graph;

        Point _mouse;

        public Form1(SubregionGraph graph) : this()
        {
            _graph = graph;
            _seed = new Random().Next();
        }

        public Form1()
        {
            InitializeComponent();

            Button btnStart = new Button();
            btnStart.Text = "Start";
            btnStart.Location = new Point(5, 5);
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);

            Button btnStepPlus = new Button();
            btnStepPlus.Text = "+";
            btnStepPlus.Location = new Point(5, btnStart.Bottom + 5);
            btnStepPlus.Click += BtnStepPlus_Click;
            Controls.Add(btnStepPlus);

            Button btnStepMinus = new Button();
            btnStepMinus.Text = "-";
            btnStepMinus.Location = new Point(5, btnStepPlus.Bottom + 5);
            btnStepMinus.Click += BtnStepMinus_Click;
            Controls.Add(btnStepMinus);

            Button btnStepMax = new Button();
            btnStepMax.Text = "Max";
            btnStepMax.Location = new Point(5, btnStepMinus.Bottom + 5);
            btnStepMax.Click += BtnStepMax_Click;
            Controls.Add(btnStepMax);

            MouseMove += Form1_MouseMove;

            _imageLeft = btnStart.Right + 5;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.X != _mouse.X || e.Y != _mouse.Y)
            {
                _mouse = new Point(e.X, e.Y);

                if (_locator!=null && _mouse.X >= _imageLeft && _mouse.Y >= _margin && _mouse.X < _imageLeft + _imageSize && _mouse.Y < _margin + _imageSize && _step == 0 && _initiated)
                {
                    _highlight = true;

                    double x = _geometrySize * (_mouse.X - _imageLeft + 0.5) / _imageSize;
                    double y = _geometrySize * (_mouse.Y - _margin + 0.5) / _imageSize;

                    _highlightedRegion = _locator.GetRegion(x, y);

                    _drawImage();
                }
                else if (_highlight)
                {
                    _highlight = false;
                    _drawImage();
                }                
            }
        }

        private void BtnStepMax_Click(object sender, EventArgs e)
        {
            _step = -1;
            if (_grid == null)
                _drawImage();
            else
                _drawGrid();
        }

        private void BtnStepMinus_Click(object sender, EventArgs e)
        {
            _step -= 1;
            if (_step < 0)
                _step = 0;
            if (_grid == null)
                _drawImage();
            else
                _drawGrid();
        }

        private void BtnStepPlus_Click(object sender, EventArgs e)
        {
            _step += 1;
            if (_grid == null)
                _drawImage();
            else
                _drawGrid();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            _seed = new Random().Next();
            _regionMap();
            //_hexagonalMap();
        }

        private void _regionMap()
        {
            _step = 0;
            _initiated = true;

            _regions = new List<Region>();
            if (_graph == null)
            {                
                _regions.Add(new Region(new Vector2[] { new Vector2(4.3, 0.4), new Vector2(5.1, 1.3), new Vector2(5.6, 2.3), new Vector2(3.9, 2.8), new Vector2(2.7, 0.8) }));
                _regions.Add(new Region(new Vector2[] { new Vector2(2.7, 0.8), new Vector2(3.9, 2.8), new Vector2(3.2, 3.2), new Vector2(1.1, 2.5), new Vector2(1.6, 1.4) }));
                _regions.Add(new Region(new Vector2[] { new Vector2(5.1, 1.3), new Vector2(5.8, 1.8), new Vector2(5.6, 2.3) }));
                _regions.Add(new Region(new Vector2[] { new Vector2(1.1, 2.5), new Vector2(3.2, 3.2), new Vector2(1.4, 4.1), new Vector2(0.4, 3.2) }));
                _regions.Add(new Region(new Vector2[] { new Vector2(3.9, 2.8), new Vector2(6.0, 4.1), new Vector2(2.5, 4.4), new Vector2(1.4, 4.1), new Vector2(3.2, 3.2) }));
                _regions.Add(new Region(new Vector2[] { new Vector2(6.0, 4.1), new Vector2(5.3, 5.5), new Vector2(4.4, 4.8), new Vector2(0.9, 5.5), new Vector2(2.5, 4.4) }));
                _regions.Add(new Region(new Vector2[] { new Vector2(4.4, 4.8), new Vector2(5.3, 5.5), new Vector2(4.2, 6.0), new Vector2(3.4, 5.5), new Vector2(2.3, 6.0), new Vector2(0.9, 5.5) }));
            }
            else
            {
                foreach(Subregion subregion in _graph.Subregions)
                {
                    _regions.Add(new Region(subregion.Vertices));
                }
            }

         
            RandomExt random = new RandomExt();
            Func<int[], Color> colorByVector = v => Color.FromArgb(v[0], v[1], v[2]);
            _colorByRegion = _regions.ToDictionary(r => r, r => new SolidBrush(colorByVector(random.NextVector(3, 256))));


            double top = _graph == null ? 7.1 : _graph.Height * 2;
            double bottom = _graph == null ? -0.1 : -_graph.Height;
            double right = _graph == null ? 7.1 : 2 * _graph.Width;
            double left = _graph == null ? -0.1 : -_graph.Width;
            _partition = new RegionPartition(_regions, top, bottom, left, right);
            _locator = new PointLocator<Region>(_partition, _graph == null ? random : new RandomExt(_seed));

            _drawImage();
        }

        private void _hexagonalMap()
        {
            _step = 0;
            _initiated = true;

            _grid = new HexGrid(20, 20);

            //_partition = new RegionPartition(_regions, 7.0, 0.0, 0.0, 7.0);
            //_locator = new PointLocator<Region>(_partition);

            _drawGrid();
        }

        void _drawGrid()
        {
            Bitmap image = new Bitmap(_imageSize, _imageSize);
            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);

            _geometrySize = (float)_grid.Height;
            float scale = (float)(_imageSize / _grid.Width);
            float radius = 3;
            float diameter = radius * 2;

            if (_step == 0)
            {
                foreach (HexCell cell in _grid.Cells)
                {
                    PointF[] points = cell.Vertices.Select(v => _fromVertex(v, scale)).ToArray();
                    g.DrawPolygon(Pens.Black, points);
                }

                CreateGraphics().DrawImage(image, _imageLeft, 5);
                image.Dispose();
                g.Dispose();
            }
            else if (_step == -1)
            {
                HexGridPartition partition = new HexGridPartition(_grid);
                PointLocator<HexCell> locator = new PointLocator<HexCell>(partition);
                PointF[] points = new PointF[4];
                int count = 0;

                foreach (Trapezoid trapezoid in locator.GetTrapezoids)
                {
                    count += 1;

                    points[0] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);
                    points[1] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    points[2] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    points[3] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);

                    g.DrawPolygon(Pens.Black, points);

                    foreach (PointF point in points)
                    {
                        g.FillEllipse(Brushes.Black, point.X - radius, point.Y - radius, diameter, diameter);
                    }
                }

                CreateGraphics().DrawImage(image, _imageLeft, 5);
                Console.WriteLine($"Trapezoids: {count}");
            }
            else
            {
                RandomExt random = new RandomExt(_seed);
                HexGridPartition partition = new HexGridPartition(_grid);
                PointLocator<HexCell> locator = new PointLocator<HexCell>(partition, random, _step);
                PointF[] points = new PointF[4];
                int count = 0;

                foreach (Trapezoid trapezoid in locator.GetTrapezoids)
                {
                    count += 1;

                    points[0] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);
                    points[1] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    points[2] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    points[3] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);

                    g.DrawPolygon(Pens.Black, points);

                    foreach (PointF point in points)
                    {
                        g.FillEllipse(Brushes.Black, point.X - radius, point.Y - radius, diameter, diameter);
                    }
                }

                CreateGraphics().DrawImage(image, _imageLeft, 5);
                Console.WriteLine($"Trapezoids: {count}");
            }
        }

        void _drawImage()
        {
            if (_regions == null)
                return;

            float shift = 30;
            _geometrySize = _graph == null ? 7.0f : (float)_graph.Width * 3;
            Bitmap image = new Bitmap(_imageSize, _imageSize);
            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);

            float scale = _imageSize / _geometrySize;
            float radius = 3;
            float diameter = radius * 2;

            if (_step == 0)
            {  
                foreach (Region region in _regions)
                {
                    PointF[] points = region.Vertices.Select(v => _fromVertex(v, scale, shift)).ToArray();
                    if (_highlight)
                    {
                        if (region.Equals(_highlightedRegion) || _highlightedRegion == null)
                            g.FillPolygon(_colorByRegion[region], points);
                        else
                            g.FillPolygon(Brushes.White, points);
                    }

                    g.DrawPolygon(Pens.Black, points);

                    foreach (PointF point in points)
                    {
                        g.FillEllipse(Brushes.Black, point.X - radius, point.Y - radius, diameter, diameter);
                    }
                }

                CreateGraphics().DrawImage(image, _imageLeft, 5);

                image.Dispose();
                g.Dispose();
            }
            else if (_step == -1)
            {
                //_locator = new PointLocator<Region>(_partition, _graph == null ? new RandomExt() : new RandomExt(_seed));
                PointLocator<Region> locator = new PointLocator<Region>(_partition, _graph == null ? new RandomExt() : new RandomExt(_seed));

                PointF[] points = new PointF[4];
                foreach (Trapezoid trapezoid in locator.GetTrapezoids)
                {
                    //points[0] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);
                    //points[1] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    //points[2] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    //points[3] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);

                    points[0] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Left.X), scale, shift);
                    points[1] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Right.X), scale, shift);
                    points[2] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Right.X), scale, shift);
                    points[3] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Left.X), scale, shift);

                    g.DrawPolygon(Pens.Black, points);

                    foreach (PointF point in points)
                    {
                        g.FillEllipse(Brushes.Black, point.X - radius, point.Y - radius, diameter, diameter);
                    }
                }

                CreateGraphics().DrawImage(image, _imageLeft, 5);
            }
            else
            {
                RandomExt random = new RandomExt(_seed);

                PointLocator<Region> locator = new PointLocator<Region>(_partition, random, _step);
                PointF[] points = new PointF[4];
                int count = 0;

                foreach(Trapezoid trapezoid in locator.GetTrapezoids)
                {
                    count += 1;

                    //points[0] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);
                    //points[1] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    //points[2] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Right.X), scale, _geometrySize);
                    //points[3] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Left.X), scale, _geometrySize);

                    points[0] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Left.X), scale, shift);
                    points[1] = _fromVertex(trapezoid.Top.GetIntersectionByX(trapezoid.Right.X), scale, shift);
                    points[2] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Right.X), scale, shift);
                    points[3] = _fromVertex(trapezoid.Bottom.GetIntersectionByX(trapezoid.Left.X), scale, shift);


                    g.DrawPolygon(Pens.Black, points);

                    foreach (PointF point in points)
                    {
                        g.FillEllipse(Brushes.Black, point.X - radius, point.Y - radius, diameter, diameter);
                    }
                }

                CreateGraphics().DrawImage(image, _imageLeft, 5);
                Console.WriteLine($"Trapezoids: {count}");
            }
        }

        PointF _fromVertex(Vector2 v, double scale) => new PointF((float)(v.X * scale), (float)(v.Y * scale));

        //PointF _fromVertex(Vertex v, double scale, float height) => new PointF((float)(v.X * scale), (float)((height - v.Y) * scale));

        PointF _fromVertex(Vector2 v, double scale, float shift) => new PointF((float)(v.X * scale + shift), (float)(v.Y * scale + shift));
    }
}
