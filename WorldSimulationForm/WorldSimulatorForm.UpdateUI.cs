using Topology;

namespace WorldSimulationForm
{
    public partial class WorldSimulatorForm
    {
        private void _updateUI(object? sender, EventArgs e)
        {
            if (!_generator.GenerationIsComplete) return;

            if (_generator.History.EventCount == 0)
            {
                _btnNextEvent.Text = "Next Event";
                _btnNextEvent.Enabled = !_generator.History.IsFinished;
                _currentEvent = null;
            }
            else
            {
                _btnNextEvent.Text = $"Next ({_generator.History.Turn})";
            }

            _image = null;

            if (_logForm.Visible)
            {
                _logForm.Update();
                _logForm.Focus();
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);

            if (!_generator.GenerationIsComplete ||
                _cmbGridLevel.SelectedItem == null ||
                _cmbMapMode.SelectedItem == null
                ) return;

            int gridLevel = (int)_cmbGridLevel.SelectedItem;

            HexGrid grid = _generator.GetGrid(gridLevel);

            RenderObjects objects = _cmbMapMode.SelectedItem switch
            {
                MapMode.Elevation => _elevationImage(grid),
                MapMode.Height => _heightImage(grid),
                MapMode.Temperature => _temperatureImage(),
                MapMode.Precipitation => _precipitationImage(),
                MapMode.Biomes => _biomesImage(),
                MapMode.Pops => _popImage(),
                MapMode.Cells => _cellsImage(),
                MapMode.Landmasses => _landmassImage(),
                _ => throw new Exception()
            };            

            if (_multiplier != 0)
            {
                objects.Multiplier = Math.Pow(2, _multiplier);
                objects.Origin = _origin;
            }

            int imageMaxWidth = (int)(ClientSize.Width * (1 - _panelWidth) - _margin * 3);
            int imageMaxHeight = ClientSize.Height - _margin * 2;
            _image = _image ?? HexGridRenderer.Render(grid, imageMaxWidth, imageMaxHeight, objects);
            e.Graphics.DrawImage(_image, _imageLeft, _margin);

            Bitmap? overlay = null;
            if (_newHighlight)
                if (_highlightedRegion != null)
                    overlay = HexGridRenderer.Render(grid, _image.Width, _image.Height, _regionOutline(_highlightedRegion));
                else if (_highlightedArea != null)
                    overlay = HexGridRenderer.Render(grid, _image.Width, _image.Height, _areaOutline(_highlightedArea));                            
            if (overlay != null)
                e.Graphics.DrawImage(overlay, _imageLeft, _margin);

            _newHighlight = false;
        }
    }
}