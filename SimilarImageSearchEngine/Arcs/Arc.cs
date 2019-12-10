using System;
using System.Collections.Generic;
using System.Text;
using AForge;

namespace SimilarImageSearch.Engine.Arcs
{
    public class Arc : ICloneable
    {
        private float _deflection, _radius, _angle, _arcLength;
        private float _tangentAngle;
        private Point _centerPoint, _endPoint;

        public const float MIN_DEFLECTION = -1;
        public const float MAX_DEFLECTION = 1;
        public const float MIN_RADIUS = 1;
        public const float PI = (float)Math.PI;
        public static readonly Point EmptyPoint = new Point(float.NaN, float.NaN);
        public static readonly IntPoint EmptyIntPoint = new IntPoint();

        public Dictionary<Arc, float> NextArcsAndAngles { get; set; }

        public Dictionary<Arc, float> PreviousArcsAndAngles { get; set; }

        public Dictionary<Arc, float> HeadToHeadArcsAndAngles { get; set; }

        public Dictionary<Arc, float> TailToTailArcsAndAngles { get; set; }

        public IntPoint StartPoint { get; private set; }

        public Point EndPoint
        {
            get { return _endPoint; }
        }

        /// <summary>
        /// Gets the center point of the circle that arc is a piece of it.
        /// </summary>
        public Point CenterPoint
        {
            get { return _centerPoint; }
        }

        public float TangentAngle
        {
            get { return _tangentAngle; }
            set
            {
                if (value > 2 * PI)
                    value -= 2 * PI;
                else if (value < 0)
                    value += 2 * PI;

                _tangentAngle = value;
                CalculateRadiusAndCenterPoint(StartPoint, _tangentAngle, _deflection, out _radius, out _centerPoint);

                if (!float.IsNaN(this.Angle))
                    this._endPoint = CalculateEndPoint(this.CenterPoint, this.Radius, this.StartAngle, this.Angle);
            }
        }

        public float StartAngle
        {
            get { return _tangentAngle - PI / 2; }
            set { TangentAngle = value + PI / 2; }
        }

        public float EndAngle
        {
            get { return StartAngle + Angle; }
        }

        public float EndPointTangentAngle
        {
            get { return _tangentAngle + _angle; }
        }

        public float Deflection
        {
            get { return _deflection; }
            set
            {
                CheckDeflection(value);
                _deflection = value;
                CalculateRadiusAndCenterPoint(StartPoint, _tangentAngle, _deflection, out _radius, out _centerPoint);
            }
        }

        public float Radius
        {
            get { return _radius; }
        }

        public float ArcLength
        {
            get { return _arcLength; }
            set
            {
                _arcLength = value;
                _angle = CalculateAngle(_arcLength, _radius);
                if (this.IsStrightLine)
                    _endPoint = new Point(this.StartPoint.X + _arcLength * (float)Math.Cos(_tangentAngle), this.StartPoint.Y + _arcLength * (float)Math.Sin(_tangentAngle));
                else
                    _endPoint = CalculateEndPoint(_centerPoint, _radius, StartAngle, _angle);
            }
        }


        public float Angle
        {
            get
            { return _angle; }
        }

        public bool IsStrightLine
        {
            get { return Deflection == 0; }
        }

        public Point MiddlePoint
        {
            get
            {
                Point res;
                if (IsStrightLine)
                {
                    res = (StartPoint + EndPoint) / 2;
                }
                else
                {
                    float midAngle = (StartAngle + EndAngle) / 2;
                    res = new Point(CenterPoint.X + (float)Math.Cos(midAngle) * Radius, CenterPoint.Y + (float)Math.Sin(midAngle) * Radius);
                }
                return res;
            }
        }

        protected Arc()
        {
            _deflection = _radius = _angle = _arcLength = float.NaN;
            _tangentAngle = float.NaN;
            _centerPoint = _endPoint = EmptyPoint;
        }

        public Arc(IntPoint StartPoint)
            : this()
        {
            this.StartPoint = StartPoint;
        }

        public Arc(IntPoint StartPoint, float TangentAngle, float Deflection)
            : this(StartPoint)
        {
            _tangentAngle = TangentAngle;
            CheckDeflection(Deflection);
            this._deflection = Deflection;
            CalculateRadiusAndCenterPoint(StartPoint, _tangentAngle, _deflection, out _radius, out _centerPoint);
        }


        public Arc(Point CenterPoint, float Radius, float StartAngle, float Angle)
            : this()
        {
            CheckRadius(Radius);
            this._centerPoint = CenterPoint;
            this._radius = Radius;
            this._tangentAngle = StartAngle + PI / 2;
            this._angle = Angle;
            this.StartPoint = new IntPoint((int)Math.Round(CenterPoint.X + Radius * Math.Cos(StartAngle)), (int)Math.Round(CenterPoint.Y + Radius * Math.Sin(StartAngle)));
            this._endPoint = CalculateEndPoint(CenterPoint, Radius, StartAngle, Angle);
            this._deflection = Radius - (float)Math.Sqrt(Radius * Radius - 1);
            this._arcLength = Radius * Angle;
        }

        public Arc(Point StartPoint, Point MiddlePoint, Point EndPoint)
            : this()
        {
            this.StartPoint = (IntPoint)StartPoint;
            this._endPoint = EndPoint;
            try
            {
                this._centerPoint = GeometryTools.CircleCenter(StartPoint, MiddlePoint, EndPoint);
                this._radius = (float)GeometryTools.Distance(StartPoint, this._centerPoint);
                float startAngle = (float)Math.Atan2(StartPoint.Y - this._centerPoint.Y, StartPoint.X - this._centerPoint.X);
                float endAngle = (float)Math.Atan2(EndPoint.Y - this._centerPoint.Y, EndPoint.X - this._centerPoint.X);
                float angleDifference = (float)GeometryTools.AngleDifference(endAngle, startAngle);
                if (angleDifference > 0)
                {
                    this._tangentAngle = startAngle + PI / 2;
                    this._angle = angleDifference;
                }
                else
                {
                    this._tangentAngle = endAngle + PI / 2;
                    this._angle = -angleDifference;
                    this.StartPoint = (IntPoint)EndPoint;
                    this._endPoint = StartPoint;
                }
                this._arcLength = this._radius * this._angle;
                this._deflection = Radius - (float)Math.Sqrt(Radius * Radius - 1);
            }
            //for straight line:
            catch (GeometryException)
            {
                this._tangentAngle = (float)Math.Atan2(EndPoint.Y - StartPoint.Y, EndPoint.X - StartPoint.X);
                this._deflection = 0;
                CalculateRadiusAndCenterPoint(this.StartPoint, _tangentAngle, _deflection, out _radius, out _centerPoint);
                this._arcLength = StartPoint.DistanceTo(EndPoint);
            }
        }

        private void CheckDeflection(float def)
        {
            if (def < MIN_DEFLECTION || def > MAX_DEFLECTION)
                throw new ArgumentException("Deflection must be between MIN_DEFLECTION and MAX_DEFLECTION.");
        }

        private void CheckRadius(float r)
        {
            if (r < MIN_RADIUS)
                throw new ArgumentException("Radius must be greater or equal to MIN_RADIUS.");
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("IsStrightLine=").Append(this.IsStrightLine).Append(" , ");
            sb.Append("StartPoint={").Append(this.StartPoint).Append("}");
            if (!this._endPoint.Equals(EmptyPoint))
                sb.Append(" , MiddlePoint={").Append(this.MiddlePoint).Append("} , EndPoint={").Append(this.EndPoint).Append("}");
            sb.Append(" , ArcLength=").Append(this.ArcLength);
            return sb.ToString();
        }

        //public static void CalculateTangentLengthAndDeflection(float Radius, float Angle, out float TangentLength, out float Deflection)
        //{
        //    TangentLength = (float)(Radius * Math.Sin(Angle));
        //    Deflection = (float)(Radius * (float)(1 - Math.Cos(Angle)));
        //}

        //public static void CalculateRadiusAndAngle(float TangentLength, float Deflection, out float Radius, out float Angle)
        //{
        //    if (Deflection == 0)
        //    {
        //        Angle = 0;
        //        Radius = float.PositiveInfinity;
        //    }
        //    else
        //    {
        //        Angle = (float)Math.Asin(2 * TangentLength * Deflection / (TangentLength * TangentLength + Deflection * Deflection));
        //        Radius = (TangentLength * TangentLength + Deflection * Deflection) / (2 * Deflection);
        //    }
        //}

        public static void CalculateRadiusAndCenterPoint(IntPoint StartPoint, float TangentAngle, float Deflection, out float Radius, out Point CenterPoint)
        {
            if (Deflection == 0)
            {
                Radius = float.PositiveInfinity;
                CenterPoint = EmptyPoint;
            }
            else
            {
                Radius = CalculateRadius(Deflection);
                float startAngle;
                if (Deflection > 0)
                    startAngle = TangentAngle + PI / 2;
                else
                    startAngle = TangentAngle - PI / 2;
                CenterPoint = new Point(StartPoint.X + Radius * (float)Math.Cos(startAngle),
                    StartPoint.Y + Radius * (float)Math.Sin(startAngle));
            }
        }

        public static float CalculateRadius(float deflection)
        {
            if (deflection == 0)
                return float.PositiveInfinity;
            return (1 + (float)Math.Pow(deflection, 2)) / (2 * Math.Abs(deflection));
        }

        public static float CalculateAngle(float ArcLength, float Radius)
        {
            return (ArcLength / Radius);
        }

        public static Point CalculateEndPoint(Point CenterPoint, float Radius, float StartAngle, float Angle)
        {
            float endAngle = StartAngle + Angle;
            return new Point(CenterPoint.X + (float)(Radius * Math.Cos(endAngle)), CenterPoint.Y + (float)(Radius * Math.Sin(endAngle)));
        }

        public object Clone()
        {
            Arc newArc = new Arc(this.StartPoint, this.TangentAngle, this.Deflection);
            newArc._angle = this._angle;
            newArc._arcLength = this._arcLength;
            newArc._centerPoint = this._centerPoint;
            newArc._endPoint = this._endPoint;

            if (this.NextArcsAndAngles != null)
            {
                newArc.NextArcsAndAngles = new Dictionary<Arc, float>();
                foreach (Arc key in this.NextArcsAndAngles.Keys)
                {
                    newArc.NextArcsAndAngles.Add(key, this.NextArcsAndAngles[key]);
                }
            }

            return newArc;
        }


        public static Arc ReverseArc(Arc arc)
        {
            if (arc.EndPoint.Equals(EmptyPoint))
                throw new Exception("EndPoint of arc is undefined.");

            Arc res = new Arc((IntPoint)arc.EndPoint, (float)GeometryTools.NormalizeAngle(arc.TangentAngle + PI), -arc.Deflection);
            res._endPoint = arc.StartPoint;

            return res;
        }
    }


    public enum ArcRelationType
    {
        Chain = 0,
        HeadToHead = 1,
        TailToTail = 2,
        Previous = 3,
    }
}
