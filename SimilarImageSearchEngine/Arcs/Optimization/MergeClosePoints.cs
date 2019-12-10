using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Math.Geometry;
using AForge;

namespace SimilarImageSearch.Engine.Arcs.Optimization
{
    public class MergeClosePoints : IArcOptimizer
    {

        public double MaxDistanceToMerge { get; set; }

        public MergeClosePoints()
        {
            this.MaxDistanceToMerge = 10;
        }

        public MergeClosePoints(double maxDistanceToMerge)
        {
            this.MaxDistanceToMerge = maxDistanceToMerge;
        }

        public ArcCollection ApplyOptimization(ArcCollection shape)
        {
            ArcCollection optimizedArcs = new ArcCollection();
            List<Point> rawPoints = new List<Point>();
            foreach (Arc arc in shape)
            {
                rawPoints.Add(arc.StartPoint);
                rawPoints.Add(arc.EndPoint);
            }
            List<Point> mergedPoints = MergePoints(rawPoints);
            foreach (Arc arc in shape)
            {
                Point MuchClosePoint2StartPoint = arc.StartPoint;
                Point MuchClosePoint2EndPoint = arc.EndPoint;
                double muchCloseDistance2StartPoint = double.MaxValue;
                double muchCloseDistance2EndPoint = double.MaxValue;
                foreach (Point p in mergedPoints)
                {
                    double distance2StartPoint = GeometryTools.Distance(p, arc.StartPoint);
                    if (distance2StartPoint < muchCloseDistance2StartPoint)
                    {
                        muchCloseDistance2StartPoint = distance2StartPoint;
                        MuchClosePoint2StartPoint = p;
                    }
                    double distance2EndPoint = GeometryTools.Distance(p, arc.EndPoint);
                    if (distance2EndPoint < muchCloseDistance2EndPoint)
                    {
                        muchCloseDistance2EndPoint = distance2EndPoint;
                        MuchClosePoint2EndPoint = p;
                    }
                }

                if (MuchClosePoint2StartPoint.Equals(MuchClosePoint2EndPoint))
                    continue;

                Arc newArc = new Arc(MuchClosePoint2StartPoint, arc.MiddlePoint, MuchClosePoint2EndPoint);
                optimizedArcs.Add(newArc);
            }
            return optimizedArcs;
        }


        private List<Point> MergePoints(List<Point> rawPoints)
        {
            List<Point> mergedPoints = new List<Point>();
            mergedPoints.AddRange(rawPoints);
            double distance = 0;
            bool mergingOccured;

            do
            {
                mergingOccured = false;
                for (int i = 0; i < mergedPoints.Count; i++)
                {
                    for (int j = i + 1; j < mergedPoints.Count; j++)
                    {
                        distance = mergedPoints[i].DistanceTo(mergedPoints[j]);
                        if (distance < MaxDistanceToMerge)
                        {
                            mergedPoints.Add((mergedPoints[i] + mergedPoints[j]) / 2);
                            mergedPoints.RemoveAt(j);
                            mergedPoints.RemoveAt(i);
                            mergingOccured = true;
                            i--;
                            break;
                        }
                    }
                }
            } while (mergingOccured);

            return mergedPoints;
        }
    }
}
