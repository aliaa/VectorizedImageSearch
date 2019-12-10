using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;

namespace SimilarImageSearch.Engine.ArcsProcessor
{
    [Serializable]
    [SqlUserDefinedType(Microsoft.SqlServer.Server.Format.Native, IsByteOrdered = true, ValidationMethodName = "ValidateArcRelation")]
    public struct SqlArcRelation : INullable
    {
        private SqlArc _arc1, _arc2;
        private static readonly SqlArcRelation _null;
        private bool _isNull;

        static SqlArcRelation()
        {
            _null = new SqlArcRelation();
            _null._isNull = true;
        }

        public SqlArc Arc1
        {
            get { return _arc1; }
            set
            {
                if (value.ValidateArc() == false)
                    throw new ArgumentException("Given Arc1 is not valid.");
                _arc1 = value;
            }
        }

        public SqlArc Arc2
        {
            get { return _arc2; }
            set
            {
                if (value.ValidateArc() == false)
                    throw new ArgumentException("Given Arc2 is not valid.");
                _arc2 = value;
            }
        }

        public int RelationType { get; set; }

        public SqlSingle AngleDifference { get; set; }

        public bool ValidateArcRelation()
        {
            return (_arc1.ValidateArc() && _arc2.ValidateArc());
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{").Append(_arc1).Append("},{").Append(_arc2).Append("},").Append(AngleDifference).Append(",").Append(RelationType);
            return sb.ToString();
        }

        [SqlMethod(OnNullCall = false)]
        public static SqlArcRelation Parse(SqlString str)
        {
            string s = str.Value;
            SqlArcRelation res = new SqlArcRelation();

            int arc1Index = s.IndexOf("{") + 1;
            string arc1Str = s.Substring(arc1Index, s.IndexOf("}") - arc1Index).Trim();
            res.Arc1 = SqlArc.Parse(arc1Str);

            int arc2Index = s.IndexOf("},{") + 3;
            string arc2Str = s.Substring(arc1Index, s.IndexOf("}", arc2Index) - arc2Index).Trim();
            res.Arc2 = SqlArc.Parse(arc1Str);

            int angIndex = s.IndexOf("},",arc2Index) + 2;
            string angStr = s.Substring(angIndex).Trim();
            res.AngleDifference = new SqlSingle(float.Parse(angStr));

            int relIndex = s.LastIndexOf(",")+1;
            string relStr = s.Substring(relIndex);
            res.RelationType = int.Parse(relStr);
            return res;
        }

        public bool IsNull
        {
            get { return _isNull; }
        }

        public static SqlArcRelation Null
        {
            get { return _null; }
        }
    }

    public enum ArcRelationType
    {
        Chain = 0,
        HeadToHead = 1,
        TailToTail = 2,
    }
}
