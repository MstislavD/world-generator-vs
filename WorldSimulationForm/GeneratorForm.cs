using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WorldSimulation;
using Topology;

namespace WorldSimulationForm
{
    [DesignerCategory("")]
    class GeneratorForm : Form
    {
        float _panelWidth = 0.05f;
        int _margin = 5;

        Rectangle _imageRect;
        Bitmap? _image;

        WorldGenerator? _generator;

        ParameterArray _gridLevel;

        public GeneratorForm()
        {
            DoubleBuffered = true;
            Visible = true;
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            Text = "World Simulator";

            _imageRect = new Rectangle();
            _imageRect.Width = (int)(ClientSize.Width * (1 - _panelWidth) - _margin * 3);
            _imageRect.Height = ClientSize.Height - _margin * 2;
            _imageRect.Location = new Point((int)(ClientSize.Width * _panelWidth + _margin * 2), _margin);

            _generator = new WorldGenerator();

            _gridLevel = new ParameterArray("Grid level", _generator.GridLevels - 1, Enumerable.Range(0, _generator.GridLevels).Cast<object>());

            ParametersPanel panel = new ParametersPanel();
            panel.Location = new Point(_margin);
            panel.Width = (int)(_panelWidth * ClientSize.Width);
            panel.AutoSize = true;
            panel.FlowDirection = FlowDirection.TopDown;
            panel.OnParameterUpdate += Panel_OnParameterUpdate;
            Controls.Add(panel);

            Button btnStart = panel.AddButton("Start");
            btnStart.Click += BtnStart_Click;

            panel.RegisterParameter(_gridLevel);
        }
        private void BtnStart_Click(object? sender, EventArgs e)
        {
            _generator?.Generate();
            _renderMap(sender, e);
        }

        private void Panel_OnParameterUpdate(object? sender, Parameter parameter)
        {
            _renderMap(sender, EventArgs.Empty);
        }

        private void _renderMap(object? sender, EventArgs e)
        {
            _image = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);

            WorldGrid? grid = _generator?.Grid((int)_gridLevel.Current);
            if (grid == null) return;

            Dictionary<Elevation, Brush> brushByElevation = new()
            {
                {Elevation.DeepOcean, Brushes.Blue },
                {Elevation.Lowland, Brushes.Green }
            };

            Random random = new Random();
            Dictionary<WorldCell, Brush> brushByContinent = new();
            foreach (WorldCell continent in _generator.Grid(0).Cells)
            {
                int delta = random.Next(106);
                if (continent.Elevation == Elevation.Lowland)
                {
                    brushByContinent[continent] = new SolidBrush(Color.FromArgb(delta, 150 + delta, 0));
                }
                else
                {
                    brushByContinent[continent] = new SolidBrush(Color.FromArgb(delta, 0, 150 + delta));
                }
            }

            RenderObjects objects = new RenderObjects();
            objects.Polygons.AddRange(grid.Cells.Select(c => new PolygonData(c, brushByElevation[c.Elevation])));
            //objects.Polygons.AddRange(grid.Cells.Select(c => new PolygonData(c, brushByContinent[Topology.TreeNode.GetRoot(c)])));

            _image = _image ?? HexGridRenderer.Render(grid, _imageRect, objects);
            e.Graphics.DrawImage(_image, _imageRect);
        }
    }
}
