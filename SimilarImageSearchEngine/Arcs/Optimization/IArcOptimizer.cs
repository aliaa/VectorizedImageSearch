using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimilarImageSearch.Engine.Arcs.Optimization
{
    public interface IArcOptimizer
    {
        ArcCollection ApplyOptimization(ArcCollection shape);
    }
}
