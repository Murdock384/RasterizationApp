using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RasterizationApp
{
    public class Polygons
    {
        public class Polygon
        {
            public List<Point> vertices = new List<Point>();
            public int thickness = 1;
            public bool isClosed = false;
            public SolidColorBrush Color = Brushes.Black;

            
            public void AddVertex(Point vertex)
            {
                vertices.Add(vertex);
            }

            
            public void ClosePolygon()
            {
                if (vertices.Count >= 3)
                {
                    isClosed = true;
                }
                
            }

            public bool IsClosed()
            {
                return isClosed;
            }




        }
    }
}
