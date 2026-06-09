using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Readscreen.Core.Models;

namespace Readscreen.Perception;

public partial class RegionPickerWindow : Window
{
    private Point _start;
    private Rectangle? _rect;
    public CaptureRegion? SelectedRegion { get; private set; }

    public RegionPickerWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        };
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(SelectionCanvas);
        _rect = new Rectangle
        {
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(60, 0, 170, 255))
        };
        Canvas.SetLeft(_rect, _start.X);
        Canvas.SetTop(_rect, _start.Y);
        SelectionCanvas.Children.Add(_rect);
        SelectionCanvas.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_rect == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(SelectionCanvas);
        var x = Math.Min(pos.X, _start.X);
        var y = Math.Min(pos.Y, _start.Y);
        var w = Math.Abs(pos.X - _start.X);
        var h = Math.Abs(pos.Y - _start.Y);

        Canvas.SetLeft(_rect, x);
        Canvas.SetTop(_rect, y);
        _rect.Width = w;
        _rect.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        SelectionCanvas.ReleaseMouseCapture();
        if (_rect == null)
            return;

        var left = (int)Canvas.GetLeft(_rect);
        var top = (int)Canvas.GetTop(_rect);
        var width = (int)_rect.Width;
        var height = (int)_rect.Height;

        if (width > 20 && height > 20)
        {
            SelectedRegion = new CaptureRegion(top, left, width, height);
            DialogResult = true;
            Close();
        }
    }
}
