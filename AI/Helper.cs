using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.DirectX;

namespace WiccanRede.AI
{
  static class Helper
  {
    /// <summary>
    /// Do Bresenham algorithm
    /// </summary>
    /// <param name="p0">start point, field in map</param>
    /// <param name="p1">end point, field in map</param>
    /// <returns>list of points, these are fields from start fiels, to end field</returns>
    public static List<Point> Bresenham(Point p0, Point p1)
    {
      List<Point> points = new List<Point>();
      //points.Add(p0);

      bool swap = Math.Abs(p1.Y - p0.Y) > Math.Abs(p1.X - p0.X);
      int x0, x1, y0, y1;
      if (swap)
      {
        x0 = p0.Y;
        x1 = p1.Y;
        y0 = p1.X;
        y1 = p0.X;
      }
      else
      {
        x0 = p0.X;
        x1 = p1.X;
        y0 = p0.Y;
        y1 = p1.Y;
      }

      if (x0 > x1)
      {
        int xTemp = x1;
        x1 = x0;
        x0 = xTemp;
        int yTemp = y1;
        y1 = y0;
        y0 = yTemp;
      }

      int deltaX = x1 - x0;
      int deltaY = Math.Abs(y1 - y0);
      int error = -(deltaX + 1) / 2;
      int yStep;
      int y = y0;

      if (y0 < y1)
      {
        yStep = 1;
      }
      else
      {
        yStep = -1;
      }

      for (int x = x0; x < x1; x++)
      {
        if (swap)
        {
          points.Add(new Point(y, x));
        }
        else
        {
          points.Add(new Point(x, y));
        }
        error += deltaY;
        if (error >= 0)
        {
          y = y + yStep;
          error -= deltaX;
        }
      }
      //points.Add(p1);
      return points;
    }

    public static int CalculateDistance(Point p0, Point p1)
    {
      int xd = p0.X - p1.X;
      int yd = p0.Y - p1.Y;

      int distance = Math.Max(Math.Abs(xd), Math.Abs(yd));
      return distance;
    }

    /// <summary>
    /// linear interpolation between two vectors3
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    public static Vector3 Interpolate3D(Vector3 start, Vector3 end, float delta)
    {
      Microsoft.DirectX.Vector3 result = new Microsoft.DirectX.Vector3();
      result.X = (1 - delta) * start.X + delta * end.X;
      result.Y = (1 - delta) * start.Y + delta * end.Y;
      result.Z = (1 - delta) * start.Z + delta * end.Z;
      return result;
    }
  }
}
