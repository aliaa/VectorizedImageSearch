using System;
using System.Collections.Generic;
using System.Text;

namespace SimilarImageSearch.Engine.Arcs
{
    public class ArcCollection : List<Arc>, ICloneable
    {
        public ArcCollection()
            :base()
        {
        }


        public object Clone()
        {
            ArcCollection res = new ArcCollection();
            foreach (Arc arc in this)
            {
                res.Add(arc);
            }
            return res;
        }
    }
}
