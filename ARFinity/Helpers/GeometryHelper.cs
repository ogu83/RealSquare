using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace ARFinity
{
    internal sealed class GeometryHelper
    {
        /// <summary>
        /// Returns the Intersection points of many rectangles
        /// </summary>
        /// <param name="R">List Of rectange to be checked</param>
        /// <returns>Intersection Points</returns>
        public static List<Point> IntersectionPoints(List<Rect> R)
        {
            List<Point> retVal = new List<Point>();

            foreach (Rect r in R)
                foreach (Rect r1 in R)
                    if (r1 != r)
                        retVal.AddRange(IntersectionPoints(r, r1));
            return null;
        }

        /// <summary>
        /// Returns the Intersection points of Rectangle and Rectangle
        /// </summary>
        /// <param name="r1">First Rectangle</param>
        /// <param name="r2">Second Rectangle</param>
        /// <returns>Intersection Points</returns>
        public static List<Point> IntersectionPoints(Rect r1, Rect r2)
        {
            List<Point> retVal = new List<Point>();
            foreach (Line r1l in RectangleLines(r1))
                retVal.AddRange(IntersectionPoints(r1l, r2));

            return retVal;
        }

        /// <summary>
        /// Returns the Intersection points of Line and Rectangle
        /// </summary>
        /// <param name="l">Line</param>
        /// <param name="r">Rectangle</param>
        /// <returns>Intersection Points</returns>
        public static List<Point> IntersectionPoints(Line l, Rect r)
        {
            List<Point> retVal = new List<Point>();

            Line[] rectLines = RectangleLines(r);

            foreach (Line rl in rectLines)
            {
                Point? i = IntersectionPoint(rl, l, true);
                if (i.HasValue)
                    retVal.Add(i.Value);
            }

            return retVal;
        }

        public static bool IsIntersected(Line l, Rect r)
        {
            Line[] rectLines = RectangleLines(r);

            foreach (Line rl in rectLines)
            {
                Point? i = IntersectionPoint(rl, l, true);
                if (i.HasValue)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a rectangle to 4 lines TOP, RIGHT,BOTTOM, LEFT line
        /// </summary>
        /// <returns>lines of rect</returns>
        private static Line[] RectangleLines(Rect r)
        {
            //List<Line> retVal = new List<Line>(4);
            Line[] retVal = new Line[4];

            Line Line1 = new Line();
            Line1.X1 = r.Left; Line1.Y1 = r.Top; Line1.X2 = r.Right; Line1.Y2 = r.Top;

            Line Line2 = new Line();
            Line2.X1 = r.Right; Line2.Y1 = r.Top; Line2.X2 = r.Right; Line2.Y2 = r.Bottom;

            Line Line3 = new Line();
            Line3.X1 = r.Right; Line3.Y1 = r.Bottom; Line3.X2 = r.Left; Line3.Y2 = r.Bottom;

            Line Line4 = new Line();
            Line4.X1 = r.Left; Line4.Y1 = r.Bottom; Line4.X2 = r.Left; Line4.Y2 = r.Top;

            retVal[0] = Line1;
            retVal[1] = Line2;
            retVal[2] = Line3;
            retVal[3] = Line4;

            return retVal;
        }

        /// <summary>
        /// returns a intersection point of direction and Line segment
        /// </summary>
        /// <param name="startPoint">direction start point</param>
        /// <param name="l">Line</param>
        /// <param name="dx">delta X</param>
        /// <param name="dy">delta Y</param>        
        /// <returns></returns>
        public static Nullable<Point> IntersectionPoint(Point startPoint, Line l, double dx, double dy)
        {
            Point? ip;

            Line l1 = new Line();
            l1.X1 = startPoint.X;
            l1.Y1 = startPoint.Y;
            l1.X2 = l1.X1 + dx;
            l1.Y2 = l1.Y1 + dy;

            ip = IntersectionPoint(l, l1, false);

            if (!ip.HasValue) return null;

            Line l2 = new Line();
            l2.X1 = startPoint.X;
            l2.Y1 = startPoint.Y;
            l2.X2 = ip.Value.X;
            l2.Y2 = ip.Value.Y;

            if (IsPointOnTheLine(l2, new Point(l1.X2, l1.Y2), 1))
                return ip;
            else
                return null;
        }


        /// <summary>
        /// distance between two points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.X - p1.X, 2));
        }

        /// <summary>
        /// Returns wether 2 lines are collided and the collision point
        /// </summary>
        /// <param name="l1">Line 1</param>
        /// <param name="l2">Line 2</param>
        /// <param name="useBoundaries">use Lines as Line Segments</param>
        /// <returns>Point of collision</returns>
        public static Nullable<Point> IntersectionPoint(Line l1, Line l2, bool useBoundaries)
        {
            double[] x = new double[6];
            double[] y = new double[6];
            double[] m = new double[3];

            x[1] = l1.X1; x[2] = l1.X2; x[4] = l2.X1; x[5] = l2.X2;
            y[1] = l1.Y1; y[2] = l1.Y2; y[4] = l2.Y1; y[5] = l2.Y2;


            if ((x[2] != x[1]) && (x[5] != x[4]))
            {
                m[1] = (y[2] - y[1]) / (x[2] - x[1]);
                m[2] = (y[5] - y[4]) / (x[5] - x[4]);
                x[3] = (x[1] * m[1] - y[1] - x[4] * m[2] + y[4]) / (m[1] - m[2]);
                y[3] = (x[3] - x[1]) * m[1] + y[1];
            }
            else if (x[1] == x[2] && x[5] != x[4])
            {
                m[2] = (y[5] - y[4]) / (x[5] - x[4]);
                x[3] = x[1];
                y[3] = (x[3] - x[4]) * m[2] + y[4];
            }
            else if (x[1] != x[2] && x[5] == x[4])
            {
                m[1] = (y[2] - y[1]) / (x[2] - x[1]);
                x[3] = x[5];
                y[3] = (x[3] - x[1]) * m[1] + y[1];
            }
            else
                return null;

            Point ip = new Point(x[3], y[3]);

            if (!useBoundaries)
                return ip;
            else if (IsPointOnTheLine(l1, ip, 1) && IsPointOnTheLine(l2, ip, 1))
                return ip;
            else
                return null;
        }

        /// <summary>
        /// returns whether a point is on the line
        /// </summary>
        /// <param name="l">the line</param>
        /// <param name="p">the point</param>
        /// <returns></returns>
        public static bool IsPointOnTheLine(Line l, Point p, int precision)
        {
            return Math.Round(Math.Round(l.X2 - l.X1, precision) / Math.Round(l.Y2 - l.Y1, precision), precision) == Math.Round(Math.Round(l.X2 - p.X, precision) / Math.Round(l.Y2 - p.Y, precision), precision)
                    &&
                   (l.X2 >= p.X && p.X >= l.X1 || l.X1 >= p.X && p.X >= l.X2)
                    &&
                   (l.Y2 >= p.Y && p.Y >= l.Y1 || l.Y1 >= p.Y && p.Y >= l.Y2);
        }
    }
}
