using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimilarImageSearch.Engine.Arcs.Optimization
{
    public class RemoveSmallArcs: IArcOptimizer
    {
        public double MinArcLength { get; set; }

        public RemoveSmallArcs(double MinArcLength)
        {
            this.MinArcLength = MinArcLength;
        }

        public ArcCollection ApplyOptimization(ArcCollection shape)
        {
            ArcCollection res = (ArcCollection)shape.Clone();
            for (int i = 0; i < res.Count; i++)
            {
                if (res[i].ArcLength <= MinArcLength)
                {
                    res.RemoveAt(i);
                    i--;
                }
            }
            return res;
        }
    }
}
