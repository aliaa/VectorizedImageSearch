using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge;

namespace SimilarImageSearch.Engine.Arcs.Optimization
{
    public class MakeSmallArcsStraight : IArcOptimizer
    {
        public int MinArcLength { get; set; }

        public MakeSmallArcsStraight(int MinArcLength)
        {
            this.MinArcLength = MinArcLength;
        }

        public ArcCollection ApplyOptimization(ArcCollection shape)
        {
            ArcCollection res = (ArcCollection)shape.Clone();
            for (int i = 0; i < shape.Count; i++)
            {
                if (res[i].ArcLength <= MinArcLength)
                {
                    //res[i] = new Arc(res[i].StartPoint, GeometryTools.CenterPoint(res[i].StartPoint, res[i].EndPoint), res[i].EndPoint);

                    float length = res[i].ArcLength;
                    Point tempEndPoint = GeometryTools.CenterPoint(res[i].MiddlePoint, res[i].EndPoint);
                    res[i] = new Arc(res[i].StartPoint, GeometryTools.CenterPoint(res[i].StartPoint, tempEndPoint), tempEndPoint);
                    res[i].ArcLength = length;

                    //double length = res[i].ArcLength;
                    //res[i] = new Arc(res[i].StartPoint, res[i].TangentAngle, 0);
                    //res[i].ArcLength = length;
                }
            }
            return res;
        }
    }
}
