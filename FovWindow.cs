using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LumyxCheatsV2
{
    public class FovWindow : Window
    {
        private Ellipse fovCircle;

        public FovWindow()
        {
            // Ustawienia przezroczystego okna nad grą
            Title = "FovWindow";
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            IsHitTestVisible = false; // Kliknięcia myszką przelatują przez okno do gry

            // Tworzenie siatki i fioletowego kółka
            Grid mainGrid = new Grid();
            fovCircle = new Ellipse
            {
                Width = 300, // Domyślna wielkość (średnica)
                Height = 300,
                Stroke = new SolidColorBrush(Color.FromRgb(139, 92, 246)), // Fioletowy fov
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            mainGrid.Children.Add(fovCircle);
            Content = mainGrid;
        }

        public void SetSize(double radius)
        {
            if (fovCircle != null)
            {
                fovCircle.Width = radius * 2;
                fovCircle.Height = radius * 2;
            }
        }
    }
}
