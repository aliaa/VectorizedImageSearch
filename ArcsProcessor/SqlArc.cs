using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Runtime.InteropServices;

namespace SimilarImageSearch.Engine.ArcsProcessor
{

    [Serializable]
    [SqlUserDefinedType(Microsoft.SqlServer.Server.Format.Native, IsByteOrdered = true, ValidationMethodName = "ValidateArc")]
    public struct SqlArc : INullable
    {
        public static readonly SqlSingle MinDeflection = -1F;
        public static readonly SqlSingle MaxDeflection = 1F;

        private static readonly SqlArc _null;
        private bool _isNull;

        static SqlArc()
        {
            _null = new SqlArc();
            _null._isNull = true;
        }

        private SqlSingle _deflection, _length;

        public SqlSingle Deflection
        {
            get { return _deflection; }
            set
            {
                SqlSingle tmp = _deflection;
                _deflection = value;
                if (ValidateArc() == false)
                {
                    _deflection = tmp;
                    throw new ArgumentException("Given deflection is not valid.");
                }
            }
        }

        public SqlSingle Length
        {
            get { return _length; }
            set
            {
                SqlSingle tmp = _length;
                _length = value;
                if (ValidateArc() == false)
                {
                    _length = tmp;
                    throw new ArgumentException("Given Length is not valid.");
                }
            }
        }

        public Int32 ArcID { get; set; }

        public bool ValidateArc()
        {
            if (_deflection < MinDeflection || _deflection > MaxDeflection || _length <= 0)
                return false;
            _isNull = false;
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ArcID).Append(",").Append(_deflection).Append(",").Append(_length);
            return sb.ToString();
        }


        [SqlMethod(OnNullCall = false)]
        public static SqlArc Parse(SqlString str)
        {
            SqlArc arc = new SqlArc();
            string[] data = str.Value.Split(',');
            arc.ArcID = int.Parse(data[0]);
            arc._deflection = new SqlSingle(float.Parse(data[1]));
            arc._length = new SqlSingle(float.Parse(data[2]));

            return arc;
        }


        public bool IsNull
        {
            get { return _isNull; }
        }

        public static SqlArc Null
        {
            get { return _null; }
        }
    }
}
