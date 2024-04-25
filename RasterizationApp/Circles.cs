using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RasterizationApp
{
    internal class Circles
    {
        public class Circle
        {
            public Point Center { get; set; }
            public int Radius;
            public SolidColorBrush Color{ get; set; }

            public Circle(Point center,int radius, SolidColorBrush color = null)
            {
                Center = center;
                Radius = radius;
                Color = color ?? Brushes.Black;
            }
        }
    }
}
