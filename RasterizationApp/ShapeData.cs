using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
namespace RasterizationApp
{
    public class ShapeData
    {
        // Define a method to serialize and save shapes to a file
        public static void SaveShapes(List<object> shapes, string filePath)
        {
            List<Dictionary<string, object>> shapeDataList = new List<Dictionary<string, object>>();

            foreach (var shape in shapes)
            {
                if (shape is Polygons.Polygon)
                {
                    Polygons.Polygon polygon = (Polygons.Polygon)shape;
                    Dictionary<string, object> polygonData = new Dictionary<string, object>
                    {
                        { "Type", "Polygon" },
                        { "Vertices", polygon.vertices },
                        { "Thickness", polygon.thickness },
                        { "Color", polygon.Color.ToString() } 
                    };
                    shapeDataList.Add(polygonData);
                }
                else if (shape is LineSegments.LineSegment)
                {
                    LineSegments.LineSegment lineSegment = (LineSegments.LineSegment)shape;
                    Dictionary<string, object> lineSegmentData = new Dictionary<string, object>
                    {
                        { "Type", "LineSegment" },
                        { "Start", lineSegment.Start },
                        { "End", lineSegment.End },
                        { "Thickness", lineSegment.thickness },
                        { "Color", lineSegment.Color.ToString() } 
                    };
                    shapeDataList.Add(lineSegmentData);
                }
                else if (shape is Circles.Circle)
                {
                    Circles.Circle circle = (Circles.Circle)shape;
                    Dictionary<string, object> circleData = new Dictionary<string, object>
                    {
                        { "Type", "Circle" },
                        { "Center", circle.Center },
                        { "Radius", circle.Radius },
                        { "Color", circle.Color.ToString() } 
                    };
                    shapeDataList.Add(circleData);
                }
            }

            string json = JsonConvert.SerializeObject(shapeDataList);
            File.WriteAllText(filePath, json);
        }
    }
}
