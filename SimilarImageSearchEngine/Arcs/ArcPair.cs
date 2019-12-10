using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimilarImageSearch.Engine.Arcs
{
    public class ArcPair
    {
        public double LengthesRatio { get; set; }

        /// <summary>
        /// The angle between two arcs
        /// </summary>
        public double Angle { get; set; }
        public double Arc1Angle { get; set; }
        public double Arc2Angle { get; set; }
        public double LengthAvg { get; set; }

        public int Count { get; set; }

        public ArcPair()
        {

        }

        public ArcPair(double length1, double length2, double Angle, double Arc1Angle, double Arc2Angle, int Count)
        {
            this.Angle = Angle;
            this.Arc1Angle = Arc1Angle;
            this.Arc2Angle = Arc2Angle;
            this.LengthesRatio = Math.Max(length1, length2) / Math.Min(length1, length2);
            this.LengthAvg = (length1 + length2) / 2;
            this.Count = Count;
        }
    }
}
