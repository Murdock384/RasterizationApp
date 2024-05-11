using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
namespace RasterizationApp
{
    internal class Rectangles
    {
        public class Rectangle
        {
            public Point cornerPoint1 { get; set; }
            public Point cornerPoint2 { get; set; }
            public int Thickness { get; set; }
            public SolidColorBrush Color { get; set; }

            public Rectangle(Point startPoint, Point endPoint, int thickness, SolidColorBrush color = null)
            {
                cornerPoint1 = startPoint;
                cornerPoint2 = endPoint;
                Thickness = thickness;
                Color = color ?? Brushes.Black;
            }
        }
    }
}
