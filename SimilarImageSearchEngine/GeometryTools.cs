using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge;
using SimilarImageSearch.Engine.Arcs;

namespace SimilarImageSearch.Engine
{
    public static class GeometryTools
    {
        private const double Epsilon = 0.0000001;

        /// <summary>
        /// Calculates the circle center that crosses from 3 points.
        /// </summary>
        /// <see cref="http://en.wikipedia.org/wiki/Circumscribed_circle"/>
        /// <returns></returns>
        public static Point CircleCenter(Point A, Point B, Point C)
        {
            //double a = Distance(A, B);
            //double b = Distance(B, C);
            //double c = Distance(A, C);

            //double radius = (a * b * c) /
            //    Math.Sqrt((a + b + c) * (-a + b + c) * (a - b + c) * (a + b - c));

            double D = 2 * (A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y));
            if (Math.Abs(D) < Epsilon)
            {
                throw new GeometryException("The given 3 points are in a straight line.");
            }

            double cx = ((A.Y * A.Y + A.X * A.X) * (B.Y - C.Y) + (B.Y * B.Y + B.X * B.X) * (C.Y - A.Y) + (C.Y * C.Y + C.X * C.X) * (A.Y - B.Y)) / D;
            double cy = ((A.Y * A.Y + A.X * A.X) * (C.X - B.X) + (B.Y * B.Y + B.X * B.X) * (A.X - C.X) + (C.Y * C.Y + C.X * C.X) * (B.X - A.X)) / D;

            return new Point((float)cx, (float)cy);
        }


        public static Point CenterPoint(Point p1, Point p2)
        {
            return (p1 + p2) / 2;
        }


        public static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(Math.Abs(p1.X - p2.X), 2) + Math.Pow(Math.Abs(p1.Y - p2.Y), 2));
        }


        public static double AngleDifference(double a1, double a2)
        {
            double diff;
            while (true)
            {
                diff = Math.Abs(a1 - a2);
                if (diff > Math.PI)
                {
                    if (a1 > a2)
                        a2 += Math.PI * 2;
                    else
                        a1 += Math.PI * 2;
                }
                else
                    return a1 - a2;
            }
        }


        public static double NormalizeAngle(double angle)
        {
            if (angle < 0)
            {
                while (angle < 0)
                {
                    angle += Math.PI * 2;
                }
            }
            else
            {
                while (angle >= Math.PI * 2)
                {
                    angle -= Math.PI * 2;
                }
            }
            return angle;
        }
    }

    [Serializable]
    public class GeometryException : ApplicationException
    {
        public GeometryException() { }
        public GeometryException(string message) : base(message) { }
        public GeometryException(string message, Exception inner) : base(message, inner) { }
        protected GeometryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
