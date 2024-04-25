using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RasterizationApp
{
    internal class LineSegments
    {
        public class LineSegment
        {
            public Point Start { get; set; }
            public Point End { get; set; }

            public int thickness = 1;
            public SolidColorBrush Color { get; set; } 

            public LineSegment(Point start, Point end, int thickness = 1,SolidColorBrush color = null)
            {
                Start = start;
                End = end;
                this.thickness = thickness;
                Color = color ?? Brushes.Black;
            }
        }
    }
}
