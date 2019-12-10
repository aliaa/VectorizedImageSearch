using System;
using System.Collections.Generic;
using System.Text;
using AForge;

namespace SimilarImageSearch.Engine.Arcs.Optimization
{
    public class MergeArcs : IArcOptimizer
    {
        public double MaxAngleDifference { get; set; }
        public double MaxDeflectionDifference { get; set; }
        public double MaxPointsDistance { get; set; }

        public MergeArcs(double MaxAngleDifference, double MaxDeflectionDifference, double MaxPointsDistance)
        {
            this.MaxAngleDifference = MaxAngleDifference;
            this.MaxDeflectionDifference = MaxDeflectionDifference;
            this.MaxPointsDistance = MaxPointsDistance;
        }

        public MergeArcs()
            : this(0, 0, 0)
        { }

        public ArcCollection ApplyOptimization(ArcCollection shape)
        {
            ArcCollection arcs = (ArcCollection)shape.Clone();
            bool mergingOccured = false;

            do
            {
                mergingOccured = false;
                for (int i = 0; i < arcs.Count - 1; i++)
                {
                    Arc a1 = arcs[i];
                    for (int j = i + 1; j < arcs.Count; j++)
                    {
                        Arc a2 = arcs[j];
                        if (GeometryTools.Distance(a1.StartPoint, a2.StartPoint) < MaxPointsDistance)
                        {
                            if (Math.PI - Math.Abs(GeometryTools.AngleDifference(a1.TangentAngle, a2.TangentAngle)) < MaxAngleDifference)
                            {
                                if (Math.Abs(a1.Deflection - a2.Deflection) < MaxDeflectionDifference)
                                {
                                    Arc mergedArc = Merge2Arcs(a1, a2, false, false);
                                    arcs.RemoveAt(j);
                                    arcs.RemoveAt(i);
                                    arcs.Add(mergedArc);
                                    i--;
                                    mergingOccured = true;
                                    //yield return arcs;
                                    break;
                                }
                            }
                        }
                        if (GeometryTools.Distance(a1.StartPoint, a2.EndPoint) < MaxPointsDistance)
                        {
                            if (Math.Abs(GeometryTools.AngleDifference(a1.TangentAngle, a2.EndPointTangentAngle)) < MaxAngleDifference)
                            {
                                if (Math.Abs(a1.Deflection - a2.Deflection) < MaxDeflectionDifference)
                                {
                                    Arc mergedArc = Merge2Arcs(a1, a2, false, true);
                                    arcs.RemoveAt(j);
                                    arcs.RemoveAt(i);
                                    arcs.Add(mergedArc);
                                    i--;
                                    mergingOccured = true;
                                    //yield return arcs;
                                    break;
                                }
                            }
                        }
                        if (GeometryTools.Distance(a1.EndPoint, a2.StartPoint) < MaxPointsDistance)
                        {
                            if (Math.Abs(GeometryTools.AngleDifference(a1.EndPointTangentAngle, a2.TangentAngle)) < MaxAngleDifference)
                            {
                                if (Math.Abs(a1.Deflection - a2.Deflection) < MaxDeflectionDifference)
                                {
                                    Arc mergedArc = Merge2Arcs(a1, a2, true, false);
                                    arcs.RemoveAt(j);
                                    arcs.RemoveAt(i);
                                    arcs.Add(mergedArc);
                                    i--;
                                    mergingOccured = true;
                                    //yield return arcs;
                                    break;
                                }
                            }
                        }
                        if (GeometryTools.Distance(a1.EndPoint, a2.EndPoint) < MaxPointsDistance)
                        {
                            if (Math.PI - Math.Abs(GeometryTools.AngleDifference(a1.EndPointTangentAngle, a2.EndPointTangentAngle)) < MaxAngleDifference)
                            {
                                if (Math.Abs(a1.Deflection - a2.Deflection) < MaxDeflectionDifference)
                                {
                                    Arc mergedArc = Merge2Arcs(a1, a2, true, true);
                                    arcs.RemoveAt(j);
                                    arcs.RemoveAt(i);
                                    arcs.Add(mergedArc);
                                    i--;
                                    mergingOccured = true;
                                    //yield return arcs;
                                    break;
                                }
                            }
                        }
                    }
                }

            } while (mergingOccured);

            RemoveDublicateArcs(ref arcs);

            return arcs;
        }



        private Arc Merge2Arcs(Arc a1, Arc a2, bool a1MergePos, bool a2MergePos)
        {
            Point startPoint, middlePoint, endPoint;

            if (a1MergePos == false && a2MergePos == false)
            {
                startPoint = a1.EndPoint;
                middlePoint = GeometryTools.CenterPoint(a1.StartPoint, a2.StartPoint);
                endPoint = a2.EndPoint;
            }
            else if (a1MergePos == false && a2MergePos == true)
            {
                startPoint = a1.EndPoint;
                middlePoint = GeometryTools.CenterPoint(a1.StartPoint, a2.EndPoint);
                endPoint = a2.StartPoint;
            }
            else if (a1MergePos == true && a2MergePos == false)
            {
                startPoint = a1.StartPoint;
                middlePoint = GeometryTools.CenterPoint(a1.EndPoint, a2.StartPoint);
                endPoint = a2.EndPoint;
            }
            else //if(a1MergePos == true && a2MergePos == true)
            {
                startPoint = a1.StartPoint;
                middlePoint = GeometryTools.CenterPoint(a1.EndPoint, a2.EndPoint);
                endPoint = a2.StartPoint;
            }
            Point averageMiddlePoint = (a1.MiddlePoint * a1.ArcLength + a2.MiddlePoint * a2.ArcLength + middlePoint * (a1.ArcLength + a2.ArcLength) / 2) / (1.5F * (a1.ArcLength + a2.ArcLength));
            return new Arc(startPoint, averageMiddlePoint, endPoint);
        }


        private void RemoveDublicateArcs(ref ArcCollection shape)
        {
            for (int i = 0; i < shape.Count; i++)
            {
                for (int j = i+1; j < shape.Count; j++)
                {
                    if (
                        ((GeometryTools.Distance(shape[i].StartPoint, shape[j].StartPoint) < 1 && GeometryTools.Distance(shape[i].EndPoint, shape[j].EndPoint) < 1)
                        ||
                        (GeometryTools.Distance(shape[i].StartPoint, shape[j].EndPoint) < 1 && shape[i].EndPoint.DistanceTo(shape[j].StartPoint) < 1))
                        && GeometryTools.Distance(shape[i].MiddlePoint, shape[j].MiddlePoint) < 1)
                    {
                        shape.RemoveAt(j);
                        j--;
                    }
                }
            }
        }
    }
}
