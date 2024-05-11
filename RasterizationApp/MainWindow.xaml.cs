using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static RasterizationApp.Circles;
using static RasterizationApp.Polygons;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
namespace RasterizationApp

{
    public partial class MainWindow : Window
    {
        private List<LineSegments.LineSegment> lineSegments = new List<LineSegments.LineSegment>();
        private Point startPoint;
        private Point endPoint;
        private bool isDrawing = false;
        private LineSegments.LineSegment selectedSegment = null;
        private string figure = "";

        private List<Circle> circles = new List<Circle>();
        private Point circleCenter; 
        private bool isFirstClick = true; 
        private Circle selectedCircle = null;

        
        private Point capsuleCenter1;
        private Point capsuleCenter2;
        private int capsuleRadius;
     
        private bool firstCapsuleClick = true;
        private bool secondCapsuleClick = false;
        private bool thirdCapsuleClick = false;
    



        private Polygons.Polygon currentPolygon;
        private Polygons.Polygon selectedPolygon;
        private List<Polygons.Polygon> polygons = new List<Polygons.Polygon>();
        private bool isMovingVertex = false;
        private int selectedVertexIndex = -1;
        private bool isMovingEdge = false;
        private List<Rectangles.Rectangle> rectangles = new List<Rectangles.Rectangle>();
        private bool isFirstRectanglePoint = true;
   
        private Rectangles.Rectangle selectedRectangle;
        private WriteableBitmap writableBitmap;
        private int width = 1200;
        private int height = 800;



        private bool antiAliasing = false;
        public MainWindow()
        {
            InitializeComponent();
            InitializeWritableBitmap();
            
            PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void InitializeWritableBitmap()
        {
            writableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            ImageCanvas.Source = writableBitmap;
            SetBitmapColor(writableBitmap, Colors.White);
        }
       
        public static void SetBitmapColor(WriteableBitmap bitmap, System.Windows.Media.Color color)
        {
            int bytesPerPixel = (bitmap.Format.BitsPerPixel + 7) / 8;
            int stride = bitmap.PixelWidth * bytesPerPixel;
            int size = stride * bitmap.PixelHeight;
            byte[] pixels = new byte[size];

            for (int i = 0; i < size; i += bytesPerPixel)
            {
                pixels[i] = color.B;
                pixels[i + 1] = color.G;
                pixels[i + 2] = color.R;
                pixels[i + 3] = color.A;
            }

            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), pixels, stride, 0);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clickedPoint = e.GetPosition(ImageCanvas);
            if (figure == "lines")
            {
               

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    
                    foreach (LineSegments.LineSegment segment in lineSegments)
                    {
                        if (PointIsCloseToLine(clickedPoint, segment.Start, segment.End))
                        {
                            
                            if (selectedSegment == segment)
                                selectedSegment = null; 
                            else
                                selectedSegment = segment;

                            RedrawCanvas();
                            return;
                        }
                    }
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    
                    if (selectedSegment != null)
                    {
                        selectedSegment.End = clickedPoint;
                        RedrawCanvas();
                        return;
                    }

             
                    if (!isDrawing)
                    {
                        startPoint = clickedPoint;
                        isDrawing = true;
                    }
                    else
                    {
                        endPoint = clickedPoint;
                        if(antiAliasing == true)
                        {
                            ThickAntialiasedLine(startPoint, endPoint, 1,Brushes.Black);
                        }
                        else
                        {
                           DrawLineDDA(startPoint, endPoint, 1);
                        }
                        
                        lineSegments.Add(new LineSegments.LineSegment(startPoint, endPoint));
                        isDrawing = false;
                    }
                }
                else if (e.ChangedButton == MouseButton.XButton1)
                {
                    if(selectedSegment != null)
                    {
                        isMovingVertex  = true;
                    }
                    
                }
            }
            else if(figure == "circles")
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {

                    if(selectedCircle == null)
                    {
                        selectedCircle = PointIsCloseToCircle(clickedPoint);
                        
                    }
                    else
                    {
                        selectedCircle = null;
                    }
                    RedrawCanvas();
                    return;




                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (selectedCircle != null)
                    {
                        selectedCircle.Center = clickedPoint;
                        RedrawCanvas();
                        return;
         
                    }
                    if (selectedCircle == null && isFirstClick == true)
                    {
                        circleCenter = clickedPoint;
                        isFirstClick = false;
                    }
                    else if(selectedCircle == null && isFirstClick == false)
                    {
                        int radius = (int)(Math.Sqrt(Math.Pow(clickedPoint.X - circleCenter.X, 2) + Math.Pow(clickedPoint.Y - circleCenter.Y, 2)));
                        circles.Add(new Circle(circleCenter, radius));
                        DrawCircleMidpoint(circleCenter, radius, Brushes.Black);
                        isFirstClick = true;
                    }
                }
                else if (e.ChangedButton == MouseButton.XButton1)
                {
                    int radius = (int)(Math.Sqrt(Math.Pow(clickedPoint.X - selectedCircle.Center.X, 2) + Math.Pow(clickedPoint.Y - selectedCircle.Center.Y, 2)));
                    selectedCircle.Radius = radius;
                    RedrawCanvas();

                }
              

            }
            else if (figure == "polygons")
            {

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if(selectedPolygon == null)
                    {
                        selectedPolygon = PointIsInsideAnyPolygon(clickedPoint);
                        currentPolygon = selectedPolygon;
                    }
                    else
                    {
                        selectedPolygon = null;
                        currentPolygon = null;
                    }
                    RedrawCanvas();
                    return;
                }
                
                else if (selectedPolygon != null && isMovingEdge)
                {
                    
                    double closestDistance = double.MaxValue;
                    Point closestEdgeStart = new Point(0,0);
                    Point closestEdgeEnd = new Point(0,0);

                    // Find the edge closest to the clicked point
                    foreach (var vertex in selectedPolygon.vertices)
                    {
                        Point startPoint = vertex;
                        Point endPoint = selectedPolygon.vertices[(selectedPolygon.vertices.IndexOf(vertex) + 1) % selectedPolygon.vertices.Count];

                        double distance = CalculateDistanceToPolygonEdge(clickedPoint, startPoint, endPoint);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestEdgeStart = startPoint;
                            closestEdgeEnd = endPoint;
                        }
                    }

                    if (closestEdgeStart != new Point(0, 0) && closestEdgeEnd != new Point(0, 0))
                    {
                        
                        double midX = (closestEdgeStart.X + closestEdgeEnd.X) / 2;
                        double midY = (closestEdgeStart.Y + closestEdgeEnd.Y) / 2;
                        Point midpoint = new Point(midX, midY);

                        
                        double deltaX = clickedPoint.X - midpoint.X;
                        double deltaY = clickedPoint.Y - midpoint.Y;
                        Point newStartPoint = new Point(closestEdgeStart.X + deltaX, closestEdgeStart.Y + deltaY);
                        Point newEndPoint = new Point(closestEdgeEnd.X + deltaX, closestEdgeEnd.Y + deltaY);

                        
                        int vertex1Index = selectedPolygon.vertices.IndexOf(closestEdgeStart);
                        int vertex2Index = selectedPolygon.vertices.IndexOf(closestEdgeEnd);
                        selectedPolygon.vertices[vertex1Index] = newStartPoint;
                        selectedPolygon.vertices[vertex2Index] = newEndPoint;

                        
                        RedrawCanvas();
                    }
                }


                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (selectedPolygon != null)
                    {
                        

                        // Calculate the translation vector
                        double dx = clickedPoint.X - currentPolygon.vertices[0].X;
                        double dy = clickedPoint.Y - currentPolygon.vertices[0].Y;

                        // Update the positions of all vertices of the selected polygon
                        for (int i = 0; i < currentPolygon.vertices.Count; i++)
                        {
                            currentPolygon.vertices[i] = new Point(currentPolygon.vertices[i].X + dx, currentPolygon.vertices[i].Y + dy);
                        }

                       
                        RedrawCanvas();
                        return;
                    }
                    if (currentPolygon == null)
                    {
                        currentPolygon = new Polygons.Polygon();
                    }


                    if (PointIsNearFirstVertex(clickedPoint, currentPolygon))
                    {
                        currentPolygon.ClosePolygon();
                        if (currentPolygon.IsClosed())
                        {
                            polygons.Add(currentPolygon);
                            DrawPolygon(currentPolygon, 1);
                            currentPolygon = null;

                        }
                        else
                        {
                            Console.WriteLine("Invalid Polygon Size is less than 3 vertices!!");
                        }
                        
                       
                    }
                    else
                    {
                        currentPolygon.AddVertex(clickedPoint);
                        // Draw line between previous and current vertex
                        if (currentPolygon.vertices.Count >= 2)
                        {
                            Point prevVertex = currentPolygon.vertices[currentPolygon.vertices.Count - 2];
                            DrawLineDDA(prevVertex, clickedPoint, 1, Brushes.Black);
                        }
                    }

                }
                else if (e.ChangedButton == MouseButton.XButton1)
                {
                    if(selectedPolygon != null)
                    {
                        for (int i = 0; i < selectedPolygon.vertices.Count; i++)
                        {
                            if (PointIsCloseToPolygonVertex(clickedPoint, selectedPolygon.vertices[i]))
                            {
                                // Set the flag indicating that a vertex is being moved and store the index of the selected vertex
                                isMovingVertex = true;
                                selectedVertexIndex = i;
                                break;
                            }
                        }


                    }
                    
                }

            }

            if (figure == "capsule")
            {
                if (firstCapsuleClick)
                {
                    
                    capsuleCenter1 = clickedPoint;
                    firstCapsuleClick = false;
                    secondCapsuleClick = true;
                }
                else if (secondCapsuleClick)
                {
                    
                    capsuleCenter2 = clickedPoint;
                    secondCapsuleClick = false;
                    thirdCapsuleClick = true;
                }
                else if (thirdCapsuleClick)
                {
                    
                    capsuleRadius = (int)Math.Sqrt(Math.Pow(capsuleCenter1.X - capsuleCenter2.X, 2) + Math.Pow(capsuleCenter1.Y - capsuleCenter2.Y, 2));

                    
                    double angle = Math.Atan2(capsuleCenter2.Y - capsuleCenter1.Y, capsuleCenter2.X - capsuleCenter1.X);

                   
                    if (angle >= -Math.PI / 4 && angle < Math.PI / 4) 
                    {
                        DrawLeftCircleHalf(capsuleCenter1, capsuleRadius, Brushes.Black);
                        DrawRightCircleHalf(capsuleCenter2, capsuleRadius, Brushes.Black);
                    }
                    else if (angle >= Math.PI / 4 && angle < 3 * Math.PI / 4) 
                    {
                        DrawUpperCircleHalf(capsuleCenter1, capsuleRadius, Brushes.Black);
                        DrawLowerCircleHalf(capsuleCenter2, capsuleRadius, Brushes.Black);
                    }
                    else if (angle >= -3 * Math.PI / 4 && angle < -Math.PI / 4) 
                    {
                        DrawLowerCircleHalf(capsuleCenter1, capsuleRadius, Brushes.Black);
                        DrawUpperCircleHalf(capsuleCenter2, capsuleRadius, Brushes.Black);
                    }
                    else // Slanting alignment
                    {
                        
                        double angle2 = Math.Atan2(capsuleCenter2.Y - capsuleCenter1.Y, capsuleCenter2.X - capsuleCenter1.X);

                       
                        double offsetX1 = capsuleRadius * Math.Cos(angle2 + Math.PI / 2);
                        double offsetY1 = capsuleRadius * Math.Sin(angle2 + Math.PI / 2);
                        double offsetX2 = capsuleRadius * Math.Cos(angle2 - Math.PI / 2);
                        double offsetY2 = capsuleRadius * Math.Sin(angle2 - Math.PI / 2);

                        
                        double rotatedX1 = capsuleCenter1.X + offsetX1;
                        double rotatedY1 = capsuleCenter1.Y + offsetY1;
                        double rotatedX2 = capsuleCenter1.X + offsetX2;
                        double rotatedY2 = capsuleCenter1.Y + offsetY2;

                        
                        DrawLeftCircleHalf(new Point((int)rotatedX1, (int)rotatedY1), capsuleRadius, Brushes.Black);
                        DrawLeftCircleHalf(new Point((int)rotatedX2, (int)rotatedY2), capsuleRadius, Brushes.Black);

                        
                        rotatedX1 = capsuleCenter2.X + offsetX1;
                        rotatedY1 = capsuleCenter2.Y + offsetY1;
                        rotatedX2 = capsuleCenter2.X + offsetX2;
                        rotatedY2 = capsuleCenter2.Y + offsetY2;

                        
                        DrawRightCircleHalf(new Point((int)rotatedX1, (int)rotatedY1), capsuleRadius, Brushes.Black);
                        DrawRightCircleHalf(new Point((int)rotatedX2, (int)rotatedY2), capsuleRadius, Brushes.Black);

                        
                        DrawLineDDA(new Point((int)rotatedX1, (int)rotatedY1), new Point((int)rotatedX2, (int)rotatedY2), 1, Brushes.Black);
                    }
                    double offsetX = capsuleRadius * Math.Cos(angle + Math.PI / 2);
                    double offsetY = capsuleRadius * Math.Sin(angle + Math.PI / 2);
                    Point startLine1 = new Point(capsuleCenter1.X + offsetX, capsuleCenter1.Y + offsetY);
                    Point endLine1 = new Point(capsuleCenter2.X + offsetX, capsuleCenter2.Y + offsetY);
                    Point startLine2 = new Point(capsuleCenter1.X - offsetX, capsuleCenter1.Y - offsetY);
                    Point endLine2 = new Point(capsuleCenter2.X - offsetX, capsuleCenter2.Y - offsetY);
                    DrawLineDDA(startLine1, endLine1, 1, Brushes.Black);
                    DrawLineDDA(startLine2, endLine2, 1, Brushes.Black);

                    // Reset for next capsule
                    thirdCapsuleClick = false;
                    firstCapsuleClick = true;
                }
            }
            if (figure == "rectangles")
            {
                
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    if (selectedRectangle == null)
                    {
                        selectedRectangle = GetSelectedRectangle(clickedPoint);
                        RedrawCanvas();
                    }
                    else
                    {
                        selectedRectangle = null;
                        RedrawCanvas();
                    }
                    return;
                }

                else if (selectedRectangle != null && (e.ChangedButton == MouseButton.XButton1))
                {
                    List<Point> selectedRectangleVertices = new List<Point>();
 
                    selectedRectangleVertices.Add(selectedRectangle.cornerPoint1);
                    selectedRectangleVertices.Add(selectedRectangle.cornerPoint2);
                    selectedRectangleVertices.Add(new Point(selectedRectangle.cornerPoint1.X, selectedRectangle.cornerPoint2.Y));
                    selectedRectangleVertices.Add(new Point(selectedRectangle.cornerPoint2.X, selectedRectangle.cornerPoint1.Y));

                    for (int i = 0; i < 4; i++)
                    {
                        if (PointIsCloseToVertex(clickedPoint, selectedRectangleVertices[i]))
                        {
                            isMovingVertex = true;
                            selectedVertexIndex = i;
                            return;
                        }
                    }

                    
                    return;
                }
                else if (selectedRectangle != null)
                {
                    MoveRectangle(clickedPoint);
                    
                }


                else if (!isMovingVertex && e.LeftButton == MouseButtonState.Pressed)
                {
                    if (selectedRectangle == null)
                    {
                        if(isFirstRectanglePoint == true)
                        {
                            startPoint = clickedPoint;
                            isFirstRectanglePoint = false;
                        }
                        else if (isFirstRectanglePoint == false)
                        {
                            endPoint = clickedPoint;
                            Rectangles.Rectangle newRectangle = new Rectangles.Rectangle(startPoint, endPoint, 1);
                            DrawFinalRectangle(newRectangle.cornerPoint1,newRectangle.cornerPoint2, newRectangle.Thickness,newRectangle.Color);
                            rectangles.Add(newRectangle);
                            isFirstRectanglePoint = true;

                        }
                     
                    }
                    
                   
                      
                    
                }
            }


        }

        private bool PointIsCloseToVertex(Point point, Point vertex)
        {
            double distance = Math.Sqrt(Math.Pow(point.X - vertex.X, 2) + Math.Pow(point.Y - vertex.Y, 2));
            return distance < 4;
        }
        private void MoveRectangle(Point clickedPoint)
        {
            double dx = clickedPoint.X - startPoint.X;
            double dy = clickedPoint.Y - startPoint.Y;

            selectedRectangle.cornerPoint1 = new Point(selectedRectangle.cornerPoint1.X + dx, selectedRectangle.cornerPoint1.Y + dy);
            selectedRectangle.cornerPoint2 = new Point(selectedRectangle.cornerPoint2.X + dx, selectedRectangle.cornerPoint2.Y + dy);

            RedrawCanvas();
        }
        private Rectangles.Rectangle GetSelectedRectangle(Point clickedPoint)
        {
            foreach (Rectangles.Rectangle rectangle in rectangles)
            {
                if (IsPointInsideRectangle(clickedPoint, rectangle))
                {
                    return rectangle;
                }
            }
            return null;
        }

        private bool IsPointInsideRectangle(Point point, Rectangles.Rectangle rectangle)
        {
            double left = Math.Min(rectangle.cornerPoint1.X, rectangle.cornerPoint2.X);
            double right = Math.Max(rectangle.cornerPoint1.X, rectangle.cornerPoint2.X);
            double top = Math.Min(rectangle.cornerPoint1.Y, rectangle.cornerPoint2.Y);
            double bottom = Math.Max(rectangle.cornerPoint1.Y, rectangle.cornerPoint2.Y);

            return (point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom);
        }



        private double CalculateDistanceToPolygonEdge(Point point, Point startPoint, Point endPoint)
        {
            
            Vector edgeVector = new Vector(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);

            
            Vector pointVector = new Vector(point.X - startPoint.X, point.Y - startPoint.Y);

            
            double t = Vector.Multiply(pointVector, edgeVector) / edgeVector.LengthSquared;

            
            t = Math.Max(0, Math.Min(1, t));

            
            Point projection = new Point(startPoint.X + t * edgeVector.X, startPoint.Y + t * edgeVector.Y);

            
            return (point - projection).Length;
        }



        private void DrawLeftCircleHalf(Point center, int radius, SolidColorBrush outlineColor = null, SolidColorBrush fillColor = null)
        {
            int x = 0;
            int y = radius;
            int d = 1 - radius;
            int dE = 3;
            int dSE = 5 - 2 * radius;

            DrawLeftCircleHalfPoints(center, x, y, outlineColor); // Draw the left half of the first half circle
            

            while (y > x)
            {

                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    y--;
                }
                x++;
                DrawLeftCircleHalfPoints(center, x, y, outlineColor);
                
            }
          
        }

        private void DrawRightCircleHalf(Point center, int radius, SolidColorBrush outlineColor = null, SolidColorBrush fillColor = null)
        {
            int x = 0;
            int y = radius;
            int d = 1 - radius;
            int dE = 3;
            int dSE = 5 - 2 * radius;

            DrawRightCircleHalfPoints(center, x, y, outlineColor); // Draw the right half of the second half circle
            
            while (y > x)
            {
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    y--;
                }
                x++;
                DrawRightCircleHalfPoints(center, x, y, outlineColor);
               
            }
           

        }

        private void DrawUpperCircleHalf(Point center, int radius, SolidColorBrush outlineColor = null, SolidColorBrush fillColor = null)
        {
            int x = 0;
            int y = radius;
            int d = 1 - radius;
            int dE = 3;
            int dSE = 5 - 2 * radius;

            DrawUpperCircleHalfPoints(center, x, y, outlineColor); // Draw the right half of the second half circle

            while (y > x)
            {
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    y--;
                }
                x++;
                DrawUpperCircleHalfPoints(center, x, y, outlineColor);
            }
        }

        private void DrawLowerCircleHalf(Point center, int radius, SolidColorBrush outlineColor = null, SolidColorBrush fillColor = null)
        {
            int x = 0;
            int y = radius;
            int d = 1 - radius;
            int dE = 3;
            int dSE = 5 - 2 * radius;

            DrawLowerCircleHalfPoints(center, x, y, outlineColor); // Draw the right half of the second half circle

            while (y > x)
            {
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    y--;
                }
                x++;
                DrawLowerCircleHalfPoints(center, x, y, outlineColor);
            }
        }

        private void DrawLeftCircleHalfPoints(Point center, int x, int y, SolidColorBrush color)
        {
            PutPixel((int)(center.X - x), (int)(center.Y + y), color);
            PutPixel((int)(center.X - y), (int)(center.Y + x), color);
            PutPixel((int)(center.X - x), (int)(center.Y - y), color);
            PutPixel((int)(center.X - y), (int)(center.Y - x), color);
        }

        private void DrawRightCircleHalfPoints(Point center, int x, int y, SolidColorBrush color)
        {
            PutPixel((int)(center.X + x), (int)(center.Y + y), color);
            PutPixel((int)(center.X + y), (int)(center.Y + x), color);
            PutPixel((int)(center.X + x), (int)(center.Y - y), color);
            PutPixel((int)(center.X + y), (int)(center.Y - x), color);
        }
        private void DrawUpperCircleHalfPoints(Point center, int x, int y, SolidColorBrush color)
        {
            PutPixel((int)(center.X + x), (int)(center.Y - y), color);
            PutPixel((int)(center.X - x), (int)(center.Y - y), color);
            PutPixel((int)(center.X + y), (int)(center.Y - x), color);
            PutPixel((int)(center.X - y), (int)(center.Y - x), color);
        }

        private void DrawLowerCircleHalfPoints(Point center, int x, int y, SolidColorBrush color)
        {
            PutPixel((int)(center.X + x), (int)(center.Y + y), color);
            PutPixel((int)(center.X - x), (int)(center.Y + y), color);
            PutPixel((int)(center.X + y), (int)(center.Y + x), color);
            PutPixel((int)(center.X - y), (int)(center.Y + x), color);
        }

     


        private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isMovingVertex && selectedPolygon != null)
            {
                selectedPolygon.vertices[selectedVertexIndex] = e.GetPosition(ImageCanvas);
                RedrawCanvas();
            }
            if (isMovingVertex && selectedSegment != null)
            {
                selectedSegment.Start = e.GetPosition(ImageCanvas);
                RedrawCanvas();
            }
            
        }
        


        private void RedrawCanvas()
        {

            InitializeWritableBitmap();
            foreach (LineSegments.LineSegment segment in lineSegments)
            {
                if(antiAliasing == false)
                {
                    if (segment == selectedSegment)
                        DrawLineDDA(segment.Start, segment.End, segment.thickness, Brushes.Red);
                    else
                        DrawLineDDA(segment.Start, segment.End, segment.thickness, segment.Color);
                }
                else
                {
                    if (segment == selectedSegment)
                    {
                        
                        ThickAntialiasedLine(segment.Start,segment.End, segment.thickness,Brushes.Red);
                    }
                    else
                    {
                        
                        ThickAntialiasedLine(segment.Start, segment.End, segment.thickness,segment.Color);
                    }
                }

            }
            foreach (Circle circle in circles)
            {
                if (circle == selectedCircle)
                    DrawCircleMidpoint(circle.Center,circle.Radius,Brushes.Red,circle.Color);
                else
                    DrawCircleMidpoint(circle.Center, circle.Radius,circle.Color,circle.Color);
            }

            foreach (Polygons.Polygon polygon in polygons)
            {
                if (polygon == selectedPolygon)
                    DrawPolygon(polygon,polygon.thickness,Brushes.Red);
                else
                    DrawPolygon(polygon,polygon.thickness,polygon.Color);
            }

            foreach (Rectangles.Rectangle rectangle in rectangles)
            {
                if (rectangle == selectedRectangle)
                    DrawFinalRectangle(rectangle.cornerPoint1,rectangle.cornerPoint2,rectangle.Thickness,Brushes.Red);
                else
                    DrawFinalRectangle(rectangle.cornerPoint1, rectangle.cornerPoint2, rectangle.Thickness, rectangle.Color);
            }
        }
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Deleting lines and circles and polygons
            if (e.Key == Key.Back && selectedSegment != null)
            {
                lineSegments.Remove(selectedSegment);
                selectedSegment = null;
                RedrawCanvas();
            }
            if (e.Key == Key.Back && selectedCircle != null)
            {
                circles.Remove(selectedCircle);
                selectedCircle = null;
                RedrawCanvas();
            }
            if (e.Key == Key.Back && selectedPolygon != null)
            {
                polygons.Remove(selectedPolygon);
                selectedPolygon = null;
                RedrawCanvas();
            }

            if (e.Key == Key.Back && selectedRectangle != null)
            {
                rectangles.Remove(selectedRectangle);
                selectedRectangle = null;
                RedrawCanvas();
            }
            //Increasing and decreasing thickness of lines
            else if (e.Key == Key.Up && selectedSegment != null)
            {
                selectedSegment.thickness += 2;
                RedrawCanvas();
            }
            else if (e.Key == Key.Down && selectedSegment != null && selectedSegment.thickness > 1)
            {
                selectedSegment.thickness -= 2; ; 
                RedrawCanvas();
            }
            //Increasing and decreasing radius of circles
            else if (e.Key == Key.Up && selectedCircle != null)
            {
                selectedCircle.Radius += 2;
                RedrawCanvas();
            }
            else if (e.Key == Key.Down && selectedCircle != null && selectedCircle.Radius > 1)
            {
                selectedCircle.Radius -= 2; ; 
                RedrawCanvas();
            }
            //Increasing and decreasing thickness of edges in polygons
            else if (e.Key == Key.Up && selectedPolygon != null)
            {
                selectedPolygon.thickness += 2;
                RedrawCanvas();
            }
            else if (e.Key == Key.Down && selectedPolygon  != null && selectedPolygon.thickness > 1)
            {
                selectedPolygon.thickness -= 2;
                RedrawCanvas();
            }
            else if (e.Key == Key.Up && selectedRectangle != null)
            {
                selectedRectangle.Thickness += 2;
                RedrawCanvas();
            }
            else if (e.Key == Key.Down && selectedRectangle != null && selectedRectangle.Thickness > 1)
            {
                selectedRectangle.Thickness -= 2;
                RedrawCanvas();
            }


            else if (e.Key == Key.M && selectedPolygon != null)
            {
                
                isMovingVertex = false;
                selectedVertexIndex = -1;
            }
            else if (e.Key == Key.M && selectedSegment != null)
            {
                
                isMovingVertex = false;
            }
            else if (e.Key == Key.M && selectedRectangle != null)
            {

                isMovingVertex = false;
            }


            if (e.Key == Key.E)
            {
                isMovingEdge = true;
            }

          
            else if (e.Key == Key.F)
            {
                isMovingEdge = false;
            }

            

        }

        
        /* https://www.geeksforgeeks.org/dda-line-generation-algorithm-computer-graphics */
        private void DrawLineDDA(Point p1, Point p2, int thickness, SolidColorBrush color = null)
        {
            if (!IsPointWithinBounds(p1) || !IsPointWithinBounds(p2))
                return;
            int x0 = (int)p1.X;
            int y0 = (int)p1.Y;
            int x1 = (int)p2.X;
            int y1 = (int)p2.Y;
            bool horizontal = false;
           
            int dx = x1 - x0;
            int dy = y1 - y0;

            int step;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                step = Math.Abs(dx);
                horizontal = true;
            }
            else
            {
                step = Math.Abs(dy);
            }
                
                

            // Calculate x-increment and y-increment for each step
            float x_incr = (float)dx / step;
            float y_incr = (float)dy / step;

            // Take the initial points as x and y
            float x = x0;
            float y = y0;

            // Draw the central line
            for (int i = 0; i < step; i++)
            {
                DrawThickPixel((int)Math.Round(x), (int)Math.Round(y), thickness,horizontal, color);
                x += x_incr;
                y += y_incr;
            }
        }

        private void DrawThickPixel(int x, int y, int thickness,bool horizontal, SolidColorBrush color)
        {
            
            PutPixel(x, y, color);

            if (thickness > 1)
            {
                if(horizontal == true)
                {
                    for (int i = 1; i <= thickness / 2; i++)
                    {
                        
                        PutPixel(x, y + i, color); // Below
                        PutPixel(x, y - i, color); // Above
                    }
                }
                else
                {
                    for (int j = 1; j <= thickness / 2; j++)
                    {
                        PutPixel(x + j, y, color); // Right
                        PutPixel(x - j, y, color); // Left
                    }
                }
                
            }
            
        }

        /*private void PutPixel(int x, int y, SolidColorBrush color = null)
        {
            Rectangle pixel = new Rectangle
            {
                Width = 1,  
                Height = 1,
                Fill = color ?? Brushes.Black
            };

            Canvas.SetLeft(pixel, x);
            Canvas.SetTop(pixel, y);
            Canvas.Children.Add(pixel);
        }*/

        private void PutPixel(int x, int y, SolidColorBrush color)
        {
            if (color == null)
            {
                
                color = Brushes.Black;
            }

            byte[] colorData = { color.Color.B, color.Color.G, color.Color.R, color.Color.A };

            int bytesPerPixel = (writableBitmap.Format.BitsPerPixel + 7) / 8;
            int stride = writableBitmap.PixelWidth * bytesPerPixel;

            writableBitmap.WritePixels(new Int32Rect(x, y, 1, 1), colorData, stride, 0);
        }
        private bool IsPointWithinBounds(Point point)
        {
            return point.X >= 0 && point.X < width && point.Y >= 0 && point.Y < height;
        }

        private bool IsBoundingBoxWithinBounds(Point center, int radius)
        {
            double left = center.X - radius;
            double top = center.Y - radius;
            double right = center.X + radius;
            double bottom = center.Y + radius;

            return left >= 0 && top >= 0 && right < width && bottom < height;
        }




        private void ThickAntialiasedLine(Point p1, Point p2, float thickness, SolidColorBrush color)
        {
            int x1 = (int)p1.X, y1 = (int)p1.Y, x2 = (int)p2.X, y2 = (int)p2.Y;
            int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
            int xDirection = x1 < x2 ? 1 : -1;
            int yDirection = y1 < y2 ? 1 : -1;

            int dE = 2 * dy, dNE = 2 * (dy - dx);
            int d = 2 * dy - dx;
            int two_v_dx = 0;
            float invDenom = 1 / (2 * (float)Math.Sqrt(dx * dx + dy * dy));
            float two_dx_invDenom = 2 * dx * invDenom;
            int x = x1, y = y1;
            int i;

            IntensifyPixel(x, y, thickness, 0, color);
            for (i = 1; IntensifyPixel(x, y + i * yDirection, thickness, i * two_dx_invDenom, color) != 0; ++i) ;
            for (i = 1; IntensifyPixel(x, y - i * yDirection, thickness, i * two_dx_invDenom, color) != 0; ++i) ;


            float slope = (float)(y2 - y1) / (x2 - x1);


            bool isMoreVertical = Math.Abs(slope) > 1;

            if (!isMoreVertical)
            {
                for (i = 1; (x + i * xDirection) != x2 && Math.Abs(y - y2) > 1 && IntensifyPixel(x + i * xDirection, y, thickness, i * two_dx_invDenom, color) != 0; ++i) ;
                for (i = 1; (x - i * xDirection) != x2 && Math.Abs(y - y2) > 1 && IntensifyPixel(x - i * xDirection, y, thickness, i * two_dx_invDenom, color) != 0; ++i) ;
            }
            else
            {
                for (i = 1; (y + i * yDirection) != y2 && IntensifyPixel(x, y + i * yDirection, thickness, i * two_dx_invDenom, color) != 0; ++i) ;
                for (i = 1; (y - i * yDirection) != y2 && IntensifyPixel(x, y - i * yDirection, thickness, i * two_dx_invDenom, color) != 0; ++i) ;
            }

            while ((x != x2 || Math.Abs(y - y2) > 1) && (x >= 0 && x < ImageCanvas.ActualWidth && y >= 0 && y < ImageCanvas.ActualHeight))
            {
                if (d < 0)
                {
                    two_v_dx = d + dx;
                    d += dE;
                }
                else
                {
                    two_v_dx = d - dx;
                    d += dNE;
                    y += yDirection;
                }
                x += xDirection;
                IntensifyPixel(x, y, thickness, two_v_dx * invDenom, color);
                for (i = 1; (y + i * yDirection) != y2 && IntensifyPixel(x, y + i * yDirection, thickness, i * two_dx_invDenom - two_v_dx * invDenom, color) != 0; ++i) ;
                for (i = 1; (y - i * yDirection) != y2 && IntensifyPixel(x, y - i * yDirection, thickness, i * two_dx_invDenom + two_v_dx * invDenom, color) != 0; ++i) ;
            }
        }




        private float IntensifyPixel(int x, int y, float thickness, float distance, SolidColorBrush lineColor)
        {
            float r = 0.5f;
            SolidColorBrush BKG_COLOR = new SolidColorBrush(Colors.White);
            float cov = Coverage(thickness, distance, r);
            if (cov > 0)
                PutPixel(x, y, Lerp(BKG_COLOR, lineColor, cov));
            return cov;
        }



        /*private float Coverage(float thickness, float distance, float r)
        {
            // Check if the line is thicker than the pixel
            if (thickness / 2 >= r)
            {
                if (distance >= -thickness / 2 && distance <= thickness / 2)
                    return CoverageInner(distance - thickness / 2, r);
                else
                    return 1 - CoverageInner(thickness - distance, r);
            }
            // The line is thinner than the pixel
            else
            {
                // Calculate coverage based on distance to the line segment
                float halfThickness = thickness / 2;
                if (distance >= -halfThickness && distance <= halfThickness)
                {

                    return 1.0f;
                }
                else if (distance < -halfThickness - r || distance > halfThickness + r)
                {

                    return 0.0f;
                }
                else
                {

                    float distanceToEdge = Math.Abs(distance) - halfThickness;
                    return CoverageInner(distanceToEdge, r);
                }
            }
        }*/

       

        private static float Coverage(float thickness, float distance, float r)
        {
            float length = Math.Abs(distance) - thickness / 2.0f;
            float clampedValue = Clamp((r - length) / r, 0.0f, 1.0f);
            return clampedValue;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }


        private SolidColorBrush Lerp(SolidColorBrush color1, SolidColorBrush color2, float t)
        {
              byte r = (byte)(color1.Color.R * (1 - t) + color2.Color.R * t);
              byte g = (byte)(color1.Color.G * (1 - t) + color2.Color.G * t);
              byte b = (byte)(color1.Color.B * (1 - t) + color2.Color.B * t);
              return new SolidColorBrush(Color.FromRgb(r, g, b));
        }




        private bool PointIsCloseToLine(Point testPoint, Point lineStart, Point lineEnd)
        {
            double distance = Math.Abs((lineEnd.Y - lineStart.Y) * testPoint.X - (lineEnd.X - lineStart.X) * testPoint.Y +
                                        lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X) /
                              Math.Sqrt(Math.Pow(lineEnd.Y - lineStart.Y, 2) + Math.Pow(lineEnd.X - lineStart.X, 2));

            
            return distance < 10;
        }







        //Circles
        private Circle PointIsCloseToCircle(Point testPoint)
        {
            Circle chosenCircle = null;

            foreach (var circle in circles.OrderBy(c => c.Radius))
            {

                double distanceToCircle = Math.Sqrt(Math.Pow(testPoint.X - circle.Center.X, 2) + Math.Pow(testPoint.Y - circle.Center.Y, 2));


                if (distanceToCircle <= circle.Radius)
                {
                    chosenCircle = circle;
                    return chosenCircle;
                }
            }
            return null;


        }
        
            
        private void DrawCircleMidpoint(Point center, int radius, SolidColorBrush outlineColor = null, SolidColorBrush fillColor = null)
        {
            if (!IsBoundingBoxWithinBounds(center, radius))
                return;

            int x = 0;
            int y = radius;
            int d = 1 - radius;
            int dE = 3;
            int dSE = 5 - 2 * radius;

            DrawCirclePoints(center, x, y, outlineColor);

            while (y > x)
            {
                if (d < 0) 
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    y--;
                }
                x++;
                DrawCirclePoints(center, x, y, outlineColor);
            }

           
            
        }


        /*private void DrawFilledCircle(Point center, int radius, SolidColorBrush fillColor)
        {
            double left = center.X - radius;
            double top = center.Y - radius;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = 2 * radius;
            ellipse.Height = 2 * radius;
            ellipse.Fill = fillColor;

            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);

            Canvas.Children.Add(ellipse);
            
        }*/


        private void DrawCirclePoints(Point center, int x, int y, SolidColorBrush color)
        {
            PutPixel((int)(center.X + x), (int)(center.Y + y), color);
            PutPixel((int)(center.X - x), (int)(center.Y + y), color);
            PutPixel((int)(center.X + x), (int)(center.Y - y), color);
            PutPixel((int)(center.X - x), (int)(center.Y - y), color);
            PutPixel((int)(center.X + y), (int)(center.Y + x), color);
            PutPixel((int)(center.X - y), (int)(center.Y + x), color);
            PutPixel((int)(center.X + y), (int)(center.Y - x), color);
            PutPixel((int)(center.X - y), (int)(center.Y - x), color);
        }

        //Polygons
        public void DrawPolygon(Polygons.Polygon polygon,int thickness,SolidColorBrush color = null)
        {
            if (polygon.vertices.Count > 2)
            {
                for (int i = 0; i < polygon.vertices.Count - 1; i++)
                {
                    DrawLineDDA(polygon.vertices[i], polygon.vertices[i + 1],thickness,color);
                }
                /*FillPolygon(polygon.vertices, polygon.Color);*/

            }
            DrawLineDDA(polygon.vertices[polygon.vertices.Count - 1],polygon.vertices[0],thickness, color);
            
        }

        /*https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/how-to-create-a-shape-using-a-streamgeometry?view=netframeworkdesktop-4.8*/
        /*private void FillPolygon(List<Point> vertices, SolidColorBrush color)
        {

            System.Windows.Shapes.Path filledPolygon = new System.Windows.Shapes.Path();
            filledPolygon.Fill = color;

            
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            figure.StartPoint = vertices[0];
            figure.IsClosed = true;

            
            PolyLineSegment polyLineSegment = new PolyLineSegment();
            for (int i = 1; i < vertices.Count; i++)
            {
                polyLineSegment.Points.Add(vertices[i]);
            }

            
            figure.Segments.Add(polyLineSegment);

            
            geometry.Figures.Add(figure);

            
            filledPolygon.Data = geometry;

            
            Canvas.Children.Add(filledPolygon);
        }*/

        private bool PointIsNearFirstVertex(Point clickedPoint, Polygons.Polygon polygon)
        {
            if (polygon.vertices.Count > 1)
            {
                Point firstVertex = polygon.vertices[0];
                double distance = Math.Sqrt(Math.Pow(clickedPoint.X - firstVertex.X, 2) + Math.Pow(clickedPoint.Y - firstVertex.Y, 2));
                return distance < 10; 
            }
            return false;
        }
        private Polygons.Polygon PointIsInsideAnyPolygon(Point clickedPoint)
        {   
            foreach (var polygon in polygons)
            {
                if (PointIsInsidePolygon(clickedPoint, polygon))
                {
                    return polygon;
                }
            }
            return null;
        }

        private bool PointIsInsidePolygon(Point point, Polygons.Polygon polygon)
        {
            int windingNumber = 0;
            int numVertices = polygon.vertices.Count;

            for (int i = 0; i < numVertices; i++)
            {
                Point currentVertex = polygon.vertices[i];
                Point nextVertex = polygon.vertices[(i + 1) % numVertices];

                if (currentVertex.Y <= point.Y)
                {
                    if (nextVertex.Y > point.Y && IsLeft(currentVertex, nextVertex, point) > 0)
                    {
                        windingNumber++;
                    }
                }
                else
                {
                    if (nextVertex.Y <= point.Y && IsLeft(currentVertex, nextVertex, point) < 0)
                    {
                        windingNumber--;
                    }
                }
            }

            return windingNumber != 0;
        }

        private bool PointIsCloseToPolygonVertex(Point clickedPoint, Point vertex)
        {
            double distance = Math.Sqrt(Math.Pow(clickedPoint.X - vertex.X, 2) + Math.Pow(clickedPoint.Y - vertex.Y, 2));
            return distance < 10;
        }
        private double IsLeft(Point P0, Point P1, Point P2)
        {
            return ((P1.X - P0.X) * (P2.Y - P0.Y) - (P2.X - P0.X) * (P1.Y - P0.Y));
        }

        //Rectangles
        private void DrawFinalRectangle(Point startPoint,Point endPoint,int thickness,SolidColorBrush color)
        {
 
            // Draw the rectangle using DrawLineDDA
            DrawLineDDA(startPoint, new Point(startPoint.X, endPoint.Y), thickness, color); // Left
            DrawLineDDA(startPoint, new Point(endPoint.X, startPoint.Y), thickness,color); // Top
            DrawLineDDA(new Point(startPoint.X, endPoint.Y), endPoint, thickness, color); // Bottom
            DrawLineDDA(new Point(endPoint.X, startPoint.Y), endPoint, thickness, color); // Right

        }
        //Applying Color 
        private void ApplySelectedColor(SolidColorBrush color)
        {
            if (selectedSegment != null)
            {
                
                selectedSegment.Color = color;
                RedrawCanvas();
            }
            else if (selectedCircle != null)
            {
                
                selectedCircle.Color = color;
                RedrawCanvas();
            }
            else if (selectedPolygon != null)
            {
                
                selectedPolygon.Color = color;
                RedrawCanvas();
            }
        }

        //Button Click Handlers
        private void DrawLinesButton_Click(object sender, RoutedEventArgs e)
        {
            figure = "lines";
        }

        private void DrawCirclesButton_Click(object sender, RoutedEventArgs e)
        {
            figure = "circles";
        }

        private void DrawPolygonsButton_Click(object sender, RoutedEventArgs e)
        {
            figure = "polygons"; 
        }
        private void Capsule_Click(object sender, RoutedEventArgs e)
        {
            figure = "capsule";
        }
        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            figure = "rectangles";
        }

        private void AntiAliasingButton_Click(object sender, RoutedEventArgs e)
        {
            if(antiAliasing == false)
            {
                antiAliasing = true;
                RedrawCanvas();
            }
            else
            {
                antiAliasing = false;
                RedrawCanvas();
            }
        }

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
           ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                System.Drawing.Color selectedColor = colorDialog.Color;

               
                Color wpfColor = Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B);
                SolidColorBrush brush = new SolidColorBrush(wpfColor);

             
                ApplySelectedColor(brush);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Create a list to hold all shapes
            List<object> shapes = new List<object>();

            // Add all shapes to the list
            shapes.AddRange(polygons);
            shapes.AddRange(lineSegments);
            shapes.AddRange(circles);

            
            SaveShapes(shapes);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                List<object> shapes = LoadShapes(json);
                // Clear existing shapes lists
                polygons.Clear();
                lineSegments.Clear();
                circles.Clear();

                // Iterate over the loaded shapes and add them to their respective lists
                foreach (var shape in shapes)
                {
                    if (shape is Polygons.Polygon)
                    {
                        polygons.Add((Polygons.Polygon)shape);
                    }
                    else if (shape is LineSegments.LineSegment)
                    {
                        lineSegments.Add((LineSegments.LineSegment)shape);
                    }
                    else if (shape is Circles.Circle)
                    {
                        circles.Add((Circles.Circle)shape);
                    }
                }
            }
                
            RedrawCanvas();
        }
        
        private List<object> LoadShapes(string json)
        {
            List<object> shapes = new List<object>();

            JArray jsonArray = JArray.Parse(json);

            foreach (JObject obj in jsonArray)
            {
                string type = obj.GetValue("Type").ToString();

                switch (type)
                {
                    case "LineSegment":
                        
                        double startX = double.Parse(obj.GetValue("Start").ToString().Split(',')[0]);
                        double startY = double.Parse(obj.GetValue("Start").ToString().Split(',')[1]);
                        double endX = double.Parse(obj.GetValue("End").ToString().Split(',')[0]);
                        double endY = double.Parse(obj.GetValue("End").ToString().Split(',')[1]);
                        double thickness = double.Parse(obj.GetValue("Thickness").ToString());
                        string color = obj.GetValue("Color").ToString();

                       
                        LineSegments.LineSegment lineSegment = new LineSegments.LineSegment(new Point(startX, startY), new Point(endX, endY), (int)thickness, ParseColor(color));
                        shapes.Add(lineSegment);
                        break;

                    case "Circle":
                        
                        double centerX = double.Parse(obj.GetValue("Center").ToString().Split(',')[0]);
                        double centerY = double.Parse(obj.GetValue("Center").ToString().Split(',')[1]);
                        double radius = double.Parse(obj.GetValue("Radius").ToString());
                        string circleColor = obj.GetValue("Color").ToString();

                        
                        Circle circle = new Circle(new Point(centerX, centerY), (int)radius, ParseColor(circleColor));
                        shapes.Add(circle);
                        break;

                    
                    case "Polygon":
                        
                        Polygons.Polygon polygon = new Polygons.Polygon();
                        JArray verticesArray = (JArray)obj.GetValue("Vertices");
                        List<Point> vertices = new List<Point>();

                        foreach (JToken vertexToken in verticesArray)
                        {
                            string[] vertexCoords = vertexToken.ToString().Split(',');
                            double vertexX = double.Parse(vertexCoords[0]);
                            double vertexY = double.Parse(vertexCoords[1]);
                            vertices.Add(new Point(vertexX, vertexY));
                        }

                        double polygonThickness = double.Parse(obj.GetValue("Thickness").ToString());
                        string polygonColor = obj.GetValue("Color").ToString();

                        
                        polygon.vertices = vertices;
                        polygon.thickness = (int)polygonThickness;
                        polygon.Color = ParseColor(polygonColor);
                        shapes.Add(polygon);
                        break;
                }
            }

            return shapes;
        }

        

        
        private static SolidColorBrush ParseColor(string colorString)
        {
            Color color = (Color)ColorConverter.ConvertFromString(colorString);
            return new SolidColorBrush(color);
        }



        
        private void SaveShapes(List<object> shapes)
        {
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                ShapeData.SaveShapes(shapes, saveFileDialog.FileName);
            }
        }

        private void CanvasClear_Click(object sender, RoutedEventArgs e)
        {
            lineSegments.Clear();
            circles.Clear();
            polygons.Clear();
            selectedCircle = null;
            selectedSegment = null;
            currentPolygon = null;
            selectedPolygon = null;
            isDrawing = false;
            isFirstClick = true;
            figure = "";
            InitializeWritableBitmap();

        }

    }


}

