using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge;
using AForge.Imaging;
using System.Drawing;

namespace CenteroidSimilarity
{
    public class CenteroidAnalysis
    {

        public static List<IntPoint> GetPointsCloud(UnmanagedImage shape)
        {
            List<IntPoint> points = new List<IntPoint>();
            for (int i = 0; i < shape.Height; i++)
            {
                for (int j = 0; j < shape.Width; j++)
                {
                    Color c = shape.GetPixel(j, i);
                    if (c.R + c.B + c.G > 384)
                        points.Add(new IntPoint(j, i));
                }
            }
            return points;
        }

        public static List<Tuple<double, int>> GetPointsCloudAnglesAndDistanceFromCenter(List<IntPoint> pointsCloud, AForge.Point center)
        {
            List<Tuple<double, int>> polar = new List<Tuple<double, int>>();
            foreach (IntPoint p in pointsCloud)
            {
                int distance = (int)Math.Round(center.DistanceTo(p));
                double angle = Math.Atan2(p.Y - center.Y, p.X - center.X);
                polar.Add(new Tuple<double, int>(angle, distance));
            }
            polar.Sort(new TupleComparer());
            return polar;
        }

        class TupleComparer : IComparer<Tuple<double, int>>
        {
            public int Compare(Tuple<double, int> x, Tuple<double, int> y)
            {
                if (x.Item1 < y.Item1)
                    return -1;
                else if (x.Item1 > y.Item1)
                    return 1;
                return 0;
            }
        }

        public enum Direction
        {
            Forward = 1,
            Backward = -1,
        }

        public static double[] DFT(double[] input, int count, Direction direction)
        {
            int N = input.Length;
            if (count > N)
                throw new Exception("count could not be greater than number of input");
            double[] output = new double[count];
            for (int i = 0; i < count; i++)
            {
                double an = 0;
                for (int t = 0; t < N; t++)
                {
                    an += input[t] * Math.Exp(- (int) direction * 2.0 * Math.PI * i * t / N);
                }
                an = an / N;
                output[i] = an;
            }
            return output;
        }
    }
}
