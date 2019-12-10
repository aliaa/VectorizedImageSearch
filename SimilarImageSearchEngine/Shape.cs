using System;
using System.Collections.Generic;
using System.Text;
using AForge.Imaging;
using AForge.Imaging.Filters;
using SimilarImageSearch.Engine.Arcs;
using SimilarImageSearch.Engine.Arcs.Optimization;
using AForge;
using System.Runtime.InteropServices;

namespace SimilarImageSearch.Engine
{
    public class Shape : IDisposable
    {

        public static System.Drawing.Size MinimumAcceptableShapeSize { get; set; }

        private const int ARC_FIT_ATTEMPT = 20;
        private const float MAX_DEFLECTION = 0.1F;
        private const byte fg = 255, bg = 0;
        private const int FIRST_FIT_TEST_COUNT = 64;

        private System.Drawing.Size DrawingOffset = new System.Drawing.Size(5, 5);
        private UnmanagedImage uimg;

        public int NeighborArcFitCount
        {
            get { return _neighborArcFitCount; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("NeighborArcFitCount must be positive.");
                _neighborArcFitCount = value;
            }
        }

        private int startY = 0;
        private int _neighborArcFitCount = 1;

        public ArcCollection Arcs { get; private set; }


        static Shape()
        {
            MinimumAcceptableShapeSize = new System.Drawing.Size(50, 50);
        }

        public Shape()
        { }


        public Shape(System.Drawing.Bitmap ShapeImage)
            : this()
        {
            this.uimg = UnmanagedImage.FromManagedImage(ShapeImage);
        }


        public Shape(UnmanagedImage uimg)
        {
            this.uimg = uimg;
        }


        public System.Drawing.Bitmap Image
        {
            get { return uimg.ToManagedImage(); }
        }


        public int Width
        {
            get { return uimg.Width; }
        }

        public int Height
        {
            get { return uimg.Height; }
        }


        public void AnalyzeArcs(int NeighborArcFitCount)
        {
            this.NeighborArcFitCount = NeighborArcFitCount;
            AnalyzeArcs();
        }


        public void AnalyzeArcs()
        {
            if (uimg == null)
                throw new NullReferenceException("uimg is null.");
            if (uimg.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                throw new Exception("Image PixelFormat must be Format8bppIndexed.");

            UnmanagedImage uimgCopy = uimg.Clone();
            this.Arcs = new ArcCollection();

            for (int y = 0; y < Height; y++)
            {
                startY = y;
                for (int x = 0; x < Width; x++)
                {
                    if (GetDataFromUimg(x, y) == fg)
                    {
                        Arc arc = FindArc(x, y);
                        if (arc.ArcLength > 0)
                        {
                            this.Arcs.Add(arc);
                            ClearArc(arc);
                        }
                    }

                }
            }
            uimg.Dispose();
            uimg = uimgCopy;
        }


        public void OptimizeArcs(IArcOptimizer arcOptimizer)
        {
            this.Arcs = arcOptimizer.ApplyOptimization(this.Arcs);
        }


        public System.Drawing.Bitmap DrawArcsToImage(bool ShowLabels)
        {
            Point maxSize = GetMaxSizeToDraw();
            System.Drawing.Bitmap drawBmp = new System.Drawing.Bitmap((int)Math.Ceiling(maxSize.X + 2 * DrawingOffset.Width), (int)Math.Ceiling(maxSize.Y + 2 * DrawingOffset.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(drawBmp);
            int i = 0;
            foreach (Arc a in Arcs)
            {
                DrawArc(a, g, ShowLabels, i, DrawingOffset);
                i++;
            }
            g.Dispose();
            return drawBmp;
        }


        private void ClearArc(Arc arc)
        {
            int x, y;
            if (arc.IsStrightLine)
            {
                for (int i = 0; i < arc.ArcLength; i++)
                {
                    for (int j = -_neighborArcFitCount; j <= _neighborArcFitCount; j++)
                    {
                        x = arc.StartPoint.X + (int)Math.Round(i * Math.Cos(arc.TangentAngle)) + (int)(j * Math.Cos(arc.TangentAngle - Math.PI / 2));
                        y = arc.StartPoint.Y + (int)Math.Round(i * Math.Sin(arc.TangentAngle)) + (int)(j * Math.Sin(arc.TangentAngle - Math.PI / 2));
                        SetDataToUimg(x, y, 0);
                    }
                }
            }
            else
            {

                for (int i = 0; i < arc.ArcLength; i++)
                {
                    double angle = i / arc.Radius;
                    double currentAngle = (arc.TangentAngle - Math.PI / 2) + angle;
                    for (int j = -_neighborArcFitCount; j <= _neighborArcFitCount; j++)
                    {
                        x = (int)Math.Round((arc.Radius + j) * Math.Cos(currentAngle) + arc.CenterPoint.X);
                        y = (int)Math.Round((arc.Radius + j) * Math.Sin(currentAngle) + arc.CenterPoint.Y);
                        SetDataToUimg(x, y, bg);
                    }
                }
            }

        }


        public byte GetDataFromUimg(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return 0;
            return Marshal.ReadByte(uimg.ImageData, y * uimg.Stride + x);
        }


        public void SetDataToUimg(int x, int y, byte value)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;
            Marshal.WriteByte(uimg.ImageData, y * uimg.Stride + x, value);
        }


        private Arc FindArc(int x, int y)
        {
            Arc arc = new Arc(new IntPoint(x, y), 0, 0);

            int fit, bestFit = -1;
            float fitVal, bestVal = 0, bestAngle = 0;
            bool tilt, bestValTilt = false;

            for (int i = 0; i < FIRST_FIT_TEST_COUNT; i++)
            {
                arc.TangentAngle = 2 * i * Arc.PI / FIRST_FIT_TEST_COUNT;

                fitVal = TestArcFit(arc, out fit, out tilt);
                if (fitVal > bestVal)
                {
                    bestVal = fitVal;
                    bestFit = fit;
                    bestValTilt = tilt;
                    bestAngle = arc.TangentAngle;
                }
            }

            float minAng = bestAngle - Arc.PI / 4;
            float maxAng = bestAngle + Arc.PI / 4;
            bestFit = -1;

            for (int i = 0; i < ARC_FIT_ATTEMPT; i++)
            {
                arc.TangentAngle = (minAng + maxAng) / 2;
                fitVal = TestArcFit(arc, out fit, out tilt);
                if (fitVal > bestFit)
                {
                    bestVal = fitVal;
                    bestFit = fit;
                    bestValTilt = tilt;
                    bestAngle = arc.TangentAngle;
                }
                if (tilt)
                    minAng = arc.TangentAngle;
                else
                    maxAng = arc.TangentAngle;
            }
            arc.TangentAngle = bestAngle;

            float minDeflection, maxDeflection;
            minDeflection = -MAX_DEFLECTION;
            maxDeflection = MAX_DEFLECTION;

            bestFit = -1;
            float bestDeflection = 0;

            for (int i = 0; i < ARC_FIT_ATTEMPT; i++)
            {
                arc.Deflection = (minDeflection + maxDeflection) / 2;
                fitVal = TestArcFit(arc, out fit, out tilt);
                if (fitVal > bestFit)
                {
                    bestVal = fitVal;
                    bestFit = fit;
                    bestValTilt = tilt;
                    bestDeflection = arc.Deflection;
                }
                if (tilt)
                    maxDeflection = arc.Deflection;
                else
                    minDeflection = arc.Deflection;
            }
            arc.Deflection = bestDeflection;
            arc.ArcLength = bestFit;

            return arc;
        }


        private float TestArcFit(Arc arc, out int fitCount, out bool tiltDirection)
        {
            if (arc.IsStrightLine)
                return TestArcFitForStrightLine(arc, out fitCount, out tiltDirection);
            else
                return TestArcFitForDefianced(arc, out fitCount, out tiltDirection);
        }


        private float TestArcFitForStrightLine(Arc arc, out int fitCount, out bool tiltDirection)
        {
            int x = arc.StartPoint.X;
            int y = arc.StartPoint.Y;
            float leftHalfValue = 0, rightHalfValue = 0, totalValue = 0;
            fitCount = 0;
            int valuelessCount = 0;
            bool hasValue = false;
            List<int> checkedPoints = new List<int>();
            while (true)
            {
                x = arc.StartPoint.X + (int)Math.Round(fitCount * Math.Cos(arc.TangentAngle));
                y = arc.StartPoint.Y + (int)Math.Round(fitCount * Math.Sin(arc.TangentAngle));
                if (GetDataFromUimg(x, y) == fg)
                {
                    hasValue = true;
                    //totalValue += 1.0 / (fitCount + 1);
                    totalValue += 1;
                    int intPoint = y * Width + x;
                    checkedPoints.Add(intPoint);
                }

                for (int i = -_neighborArcFitCount; i <= _neighborArcFitCount; i++)
                {
                    if (i == 0)
                        continue;
                    x = arc.StartPoint.X + (int)Math.Round(fitCount * Math.Cos(arc.TangentAngle)) + (int)(i * Math.Cos(arc.TangentAngle + Math.PI / 2));
                    y = arc.StartPoint.Y + (int)Math.Round(fitCount * Math.Sin(arc.TangentAngle)) + (int)(i * Math.Sin(arc.TangentAngle + Math.PI / 2));
                    if (GetDataFromUimg(x, y) == fg)
                    {

                        int intPoint = y * Width + x;
                        if (checkedPoints.Contains(intPoint))
                            continue;
                        else
                            checkedPoints.Add(intPoint);

                        hasValue = true;
                        //double val = (1.0 / (fitCount + 1)) * (double)(_neighborArcFitCount + 1 - Math.Abs(i)) / (_neighborArcFitCount + 1);
                        float val = (float)(_neighborArcFitCount + 1 - Math.Abs(i)) / (_neighborArcFitCount + 1);
                        totalValue += val;
                        if (i < 0)
                            leftHalfValue += val;
                        else if (i > 0)
                            rightHalfValue += val;
                    }
                }
                if (!hasValue)
                    valuelessCount++;
                else
                    valuelessCount = 0;
                if (valuelessCount > _neighborArcFitCount)
                    break;

                fitCount++;
                hasValue = false;
            }
            checkedPoints.Clear();
            fitCount -= _neighborArcFitCount + 1;
            tiltDirection = (rightHalfValue > leftHalfValue);
            return totalValue;
        }


        private float TestArcFitForDefianced(Arc arc, out int fitCount, out bool tiltDirection)
        {
            int x = arc.StartPoint.X;
            int y = arc.StartPoint.Y;
            float angle = 0;
            fitCount = 0;
            float innerHalfValue = 0, outerHalfValue = 0, totalValue = 0;
            int valuelessCount = 0;
            List<int> checkedPoints = new List<int>();
            bool hasValue = false;

            while (true)
            {
                angle = fitCount / arc.Radius;
                float currentAngle = arc.StartAngle + angle;
                x = (int)Math.Round(arc.Radius * Math.Cos(currentAngle) + arc.CenterPoint.X);
                y = (int)Math.Round(arc.Radius * Math.Sin(currentAngle) + arc.CenterPoint.Y);
                if (GetDataFromUimg(x, y) == fg)
                {
                    int intPoint = y * Width + x;
                    checkedPoints.Add(intPoint);
                    hasValue = true;
                    //totalValue += 1.0 / (fitCount + 1);
                    totalValue += 1;
                }

                for (int i = -_neighborArcFitCount; i <= _neighborArcFitCount; i++)
                {
                    if (i == 0)
                        continue;
                    x = (int)Math.Round((arc.Radius + i) * Math.Cos(currentAngle) + arc.CenterPoint.X);
                    y = (int)Math.Round((arc.Radius + i) * Math.Sin(currentAngle) + arc.CenterPoint.Y);
                    if (GetDataFromUimg(x, y) == fg)
                    {
                        int intPoint = y * Width + x;
                        if (checkedPoints.Contains(intPoint))
                            continue;
                        else
                            checkedPoints.Add(intPoint);

                        hasValue = true;
                        //double val = (1.0 / (fitCount + 1)) * (double)(_neighborArcFitCount + 1 - Math.Abs(i)) / (_neighborArcFitCount + 1);
                        float val = (float)(_neighborArcFitCount + 1 - Math.Abs(i)) / (_neighborArcFitCount + 1);
                        totalValue += val;
                        if (i < 0)
                            innerHalfValue += val;
                        else if (i > 0)
                            outerHalfValue += val;
                    }
                }
                if (!hasValue)
                    valuelessCount++;
                else
                    valuelessCount = 0;
                if (valuelessCount > _neighborArcFitCount)
                    break;

                fitCount++;
                hasValue = false;
            }
            checkedPoints.Clear();
            angle -= (_neighborArcFitCount + 1) / arc.Radius;

            tiltDirection = (outerHalfValue > innerHalfValue);
            return totalValue;
        }


        public void RelativateArcsWithMergingPoints()
        {
            if (this.Arcs.Count == 0)
                return;
            if (this.Arcs.Count == 1)
            {
                this.Arcs[0].NextArcsAndAngles = new Dictionary<Arc, float>();
                this.Arcs[0].PreviousArcsAndAngles = new Dictionary<Arc, float>();
                this.Arcs[0].HeadToHeadArcsAndAngles = new Dictionary<Arc, float>();
                this.Arcs[0].TailToTailArcsAndAngles = new Dictionary<Arc, float>();
                return;
            }
            MergeClosePoints merger = new MergeClosePoints();
            while (true)
            {
                ArcCollection optimizedArcs = merger.ApplyOptimization(this.Arcs);
                TryRelativatingArcsAndBridgeUnrelated(optimizedArcs);
                ArcCollection biggestIsland = TrySortingAndGetBiggestIsland(optimizedArcs);
                if (biggestIsland.Count == optimizedArcs.Count)
                {
                    this.Arcs = biggestIsland;
                    break;
                }
                merger.MaxDistanceToMerge++;
            }
        }


        public void RelativateArcsWithBridging()
        {
            ArcCollection biggestIsland;
            TryRelativatingArcsAndBridgeUnrelated();

            while (true)
            {
                biggestIsland = TrySortingAndGetBiggestIsland();
                if (biggestIsland.Count == Arcs.Count)
                    break;

                List<Point> pointsCollection1 = new List<Point>();
                List<Point> pointsCollection2 = new List<Point>();
                foreach (Arc arc in biggestIsland)
                {
                    pointsCollection1.Add(arc.StartPoint);
                    pointsCollection1.Add(arc.EndPoint);
                }
                foreach (Arc arc in Arcs)
                {
                    if (!biggestIsland.Contains(arc))
                    {
                        pointsCollection2.Add(arc.StartPoint);
                        pointsCollection2.Add(arc.EndPoint);
                    }
                }

                Point p1, p2;
                GetNearsetPointsBetween2Collections(pointsCollection1, pointsCollection2, out p1, out p2);
                Arc bridgeArc = new Arc(p1, GeometryTools.CenterPoint(p1, p2), p2);
                Arcs.Add(bridgeArc);
                TryRelativatingArcsAndGetUnrelatedArcs();
            }
        }

        
        public void TryRelativatingArcsAndBridgeUnrelated()
        {
            TryRelativatingArcsAndBridgeUnrelated(this.Arcs);
        }


        public static void TryRelativatingArcsAndBridgeUnrelated(ArcCollection refArcs)
        {
            ArcCollection unrelatedArcs;
            unrelatedArcs = TryRelativatingArcsAndGetUnrelatedArcs(refArcs);
            for (int i = 0; i < unrelatedArcs.Count; i++)
            {
                Arc arc = unrelatedArcs[i];
                foreach (Point nearPoint in GetPointsSortedByDistance(arc.StartPoint, refArcs))
                {
                    Arc bridgeArc = new Arc(nearPoint, GeometryTools.CenterPoint(nearPoint, arc.StartPoint), arc.StartPoint);
                    refArcs.Add(bridgeArc);
                    ArcCollection newUnrelatedArcs = TryRelativatingArcsAndGetUnrelatedArcs(refArcs);
                    if (newUnrelatedArcs.Count < unrelatedArcs.Count)
                    {
                        unrelatedArcs = newUnrelatedArcs;
                        i = -1;
                        break;
                    }
                    else
                    {
                        refArcs.Remove(bridgeArc);
                    }
                }
            }
        }


        public ArcCollection TrySortingAndGetBiggestIsland()
        {
            return TrySortingAndGetBiggestIsland(this.Arcs);
        }


        public static ArcCollection TrySortingAndGetBiggestIsland(ArcCollection refArcs)
        {
            //Sorting arcs with the relativities that has been found:
            ArcCollection biggestIsland = new ArcCollection();
            for (int i = 0; i < refArcs.Count; i++)
            {
                ArcCollection sortedArcs = new ArcCollection();
                sortedArcs.Add(refArcs[i]);
                for (int j = 0; j < sortedArcs.Count; j++)
                {
                    foreach (Arc arc in sortedArcs[j].NextArcsAndAngles.Keys)
                    {
                        if (!sortedArcs.Contains(arc))
                            sortedArcs.Add(arc);
                    }
                    foreach (Arc arc in sortedArcs[j].HeadToHeadArcsAndAngles.Keys)
                    {
                        if (!sortedArcs.Contains(arc))
                            sortedArcs.Add(arc);
                    }
                    foreach (Arc arc in sortedArcs[j].TailToTailArcsAndAngles.Keys)
                    {
                        if (!sortedArcs.Contains(arc))
                            sortedArcs.Add(arc);
                    }
                    foreach (Arc arc in sortedArcs[j].PreviousArcsAndAngles.Keys)
                    {
                        if (!sortedArcs.Contains(arc))
                            sortedArcs.Add(arc);
                    }
                }
                if (sortedArcs.Count > biggestIsland.Count)
                    biggestIsland = sortedArcs;

                if (sortedArcs.Count == refArcs.Count)
                {
                    refArcs = sortedArcs;
                    break;
                }
            }
            return biggestIsland;
        }


        private ArcCollection TryRelativatingArcsAndGetUnrelatedArcs()
        {
            return TryRelativatingArcsAndGetUnrelatedArcs(this.Arcs);
        }


        private static ArcCollection TryRelativatingArcsAndGetUnrelatedArcs(ArcCollection refArcs)
        {
            List<Arc> relatedArcs = new List<Arc>();
            foreach (Arc arc in refArcs)
            {
                Dictionary<Arc, float> nextArcs = new Dictionary<Arc, float>();
                foreach (Arc nextArc in GetArcsWithStartPoint(arc.EndPoint, refArcs))
                {
                    nextArcs.Add(nextArc, (float)GeometryTools.AngleDifference(nextArc.TangentAngle, arc.EndPointTangentAngle));
                    if (!relatedArcs.Contains(nextArc))
                        relatedArcs.Add(nextArc);
                }
                arc.NextArcsAndAngles = nextArcs;
            }

            foreach (Arc arc in refArcs)
            {
                Dictionary<Arc, float> prevArcs = new Dictionary<Arc, float>();
                foreach (Arc prevArc in GetArcsWithEndPoint(arc.StartPoint, refArcs))
                {
                    prevArcs.Add(prevArc, (float)GeometryTools.AngleDifference(prevArc.EndPointTangentAngle, arc.TangentAngle));
                    if (!relatedArcs.Contains(prevArc))
                        relatedArcs.Add(prevArc);
                }
                arc.PreviousArcsAndAngles = prevArcs;
            }

            foreach (Arc arc in refArcs)
            {
                Dictionary<Arc, float> head2headArcs = new Dictionary<Arc, float>();
                foreach (Arc head2headArc in GetArcsWithStartPoint(arc.StartPoint, refArcs))
                {
                    if (arc != head2headArc)
                    {
                        head2headArcs.Add(head2headArc, (float)GeometryTools.AngleDifference(head2headArc.TangentAngle, arc.TangentAngle));
                        if (!relatedArcs.Contains(head2headArc))
                            relatedArcs.Add(head2headArc);
                    }
                }
                arc.HeadToHeadArcsAndAngles = head2headArcs;
            }

            foreach (Arc arc in refArcs)
            {
                Dictionary<Arc, float> tail2tailArcs = new Dictionary<Arc, float>();
                foreach (Arc tail2tailArc in GetArcsWithEndPoint(arc.EndPoint, refArcs))
                {
                    if (arc != tail2tailArc)
                    {
                        tail2tailArcs.Add(tail2tailArc, (float)GeometryTools.AngleDifference(tail2tailArc.EndPointTangentAngle, arc.EndPointTangentAngle));
                        if (!relatedArcs.Contains(tail2tailArc))
                            relatedArcs.Add(tail2tailArc);
                    }
                }
                arc.TailToTailArcsAndAngles = tail2tailArcs;
            }

            ArcCollection unrelatedArcs = new ArcCollection();
            foreach (Arc arc in refArcs)
            {
                if (!relatedArcs.Contains(arc))
                    unrelatedArcs.Add(arc);
            }
            return unrelatedArcs;
        }


        private IEnumerable<Point> GetPointsSortedByDistance(Point refPoint)
        {
            foreach (Point p in GetPointsSortedByDistance(refPoint, this.Arcs))
            {
                yield return p;
            }
        }


        private static IEnumerable<Point> GetPointsSortedByDistance(Point refPoint, ArcCollection refArcs)
        {
            List<Point> pointsCollection = new List<Point>();
            foreach (Arc arc in refArcs)
            {
                if (!refPoint.Equals(arc))
                {
                    pointsCollection.Add(arc.StartPoint);
                    pointsCollection.Add(arc.EndPoint);
                }
            }

            foreach (Point p in GetPointsSortedByDistance(refPoint, pointsCollection))
            {
                yield return p;
            }
        }

        private static IEnumerable<Point> GetPointsSortedByDistance(Point refPoint, List<Point> pointsCollection)
        {
            Point nearest = Arc.EmptyPoint;
            double nearestDistance = double.MaxValue;
            while (pointsCollection.Count > 0)
            {
                for (int i = 0; i < pointsCollection.Count; i++)
                {
                    double dis = pointsCollection[i].DistanceTo(refPoint);
                    if (dis < nearestDistance)
                    {
                        nearestDistance = dis;
                        nearest = pointsCollection[i];
                    }
                }
                yield return nearest;
                pointsCollection.Remove(nearest);
            }
        }


        private static void GetNearsetPointsBetween2Collections(List<Point> pointsCollection1, List<Point> pointsCollection2, out Point p1, out Point p2)
        {
            p1 = p2 = Arc.EmptyPoint;
            double smallestDistance = float.MaxValue;
            foreach (Point point1 in pointsCollection1)
            {
                foreach (Point point2 in pointsCollection2)
                {
                    float dis = point1.DistanceTo(point2);
                    if (dis < smallestDistance)
                    {
                        smallestDistance = dis;
                        p1 = point1;
                        p2 = point2;
                    }
                }
            }
        }


        private static readonly double SQRT2 = Math.Sqrt(2);


        private IEnumerable<Arc> GetArcsWithStartPoint(Point startPoint)
        {
            foreach (Arc arc in GetArcsWithStartPoint(startPoint, this.Arcs))
            {
                yield return arc;
            }
        }


        private static IEnumerable<Arc> GetArcsWithStartPoint(Point startPoint, ArcCollection refArcs)
        {
            foreach (Arc arc in refArcs)
            {
                if (GeometryTools.Distance(arc.StartPoint, startPoint) < SQRT2)
                    yield return arc;
            }
        }


        private IEnumerable<Arc> GetArcsWithEndPoint(Point endPoint)
        {
            foreach (Arc arc in GetArcsWithEndPoint(endPoint, this.Arcs))
            {
                yield return arc;
            }
        }


        private static IEnumerable<Arc> GetArcsWithEndPoint(Point endPoint, ArcCollection refArcs)
        {
            foreach (Arc arc in refArcs)
            {
                if (GeometryTools.Distance(arc.EndPoint, endPoint) < SQRT2)
                    yield return arc;
            }
        }

        [Obsolete]
        public System.Drawing.Bitmap DrawArcsToImageWithRelativity(bool showLabels)
        {
            Point maxSize = GetMaxSizeToDraw();
            Point minSize = GetMinSizeToDraw();
            System.Drawing.Bitmap drawBmp = new System.Drawing.Bitmap((int)Math.Ceiling(maxSize.X - minSize.X + 2 * DrawingOffset.Width), (int)Math.Ceiling(maxSize.Y - minSize.Y + 2 * DrawingOffset.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(drawBmp);
            System.Drawing.Size oldDrawingOffset = DrawingOffset;
            DrawingOffset = new System.Drawing.Size((int)Math.Ceiling(-minSize.X), (int)Math.Ceiling(-minSize.Y));
            drawedArcs = new List<Arc>();
            DrawArcsRecrusively(Arcs[0], new IntPoint(0, 0), 0, g, showLabels, 0);
            drawedArcs.Clear();
            drawedArcs = null;
            DrawingOffset = oldDrawingOffset;

            g.Dispose();
            return drawBmp;
        }


        private List<Arc> drawedArcs;

        [Obsolete]
        private void DrawArcsRecrusively(Arc startArc, IntPoint startPoint, float tangentAngleOffset, System.Drawing.Graphics g, bool showLabels, int arcLabel)
        {
            Arc arcCopy = new Arc(startPoint, tangentAngleOffset, startArc.Deflection);
            //arcCopy.NextArcsAndAngles = startArc.NextArcsAndAngles;
            //arcCopy.HeadToHeadArcsAndAngles = startArc.HeadToHeadArcsAndAngles;
            //arcCopy.TailToTailArcsAndAngles = startArc.TailToTailArcsAndAngles;
            arcCopy.ArcLength = startArc.ArcLength;

            DrawArc(arcCopy, g, showLabels, arcLabel, DrawingOffset);
            drawedArcs.Add(startArc);

            foreach (Arc nextArc in startArc.NextArcsAndAngles.Keys)
            {
                if (!drawedArcs.Contains(nextArc))
                    DrawArcsRecrusively(nextArc, (IntPoint)arcCopy.EndPoint, (float)GeometryTools.NormalizeAngle(startArc.NextArcsAndAngles[nextArc] + arcCopy.EndPointTangentAngle), g, showLabels, drawedArcs.Count);
            }

            foreach (Arc nextArc in startArc.HeadToHeadArcsAndAngles.Keys)
            {
                if (!drawedArcs.Contains(nextArc))
                    DrawArcsRecrusively(nextArc, arcCopy.StartPoint, (float)GeometryTools.NormalizeAngle(startArc.HeadToHeadArcsAndAngles[nextArc] + arcCopy.TangentAngle), g, showLabels, drawedArcs.Count);
            }

            //foreach (Arc nextArc in startArc.TailToTailArcsAndAngles.Keys)
            //{
            //    if(!drawedArcs.Contains(nextArc))
            //        DrawArcsRecrusively(nextArc, 
            //}
        }


        private void DrawArc(Arc arc, System.Drawing.Graphics g, bool showLabels, int arcLabel, System.Drawing.Size offset)
        {
            try
            {
                int red, green, blue;
                if (arcLabel <= Arcs.Count / 2)
                {
                    red = (int)(255.0 * arcLabel / (Arcs.Count / 2));
                    green = (int)(255.0 * ((Arcs.Count / 2) - arcLabel) / (Arcs.Count / 2));
                    blue = 0;
                }
                else
                {
                    red = (int)(255.0 * (Arcs.Count - arcLabel) / (Arcs.Count / 2));
                    green = 0;
                    blue = (int)(255.0 * (arcLabel - (Arcs.Count / 2)) / (Arcs.Count / 2));
                }
                System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(red,green,blue));

                //Pen pen = new Pen(Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255)));
                //Pen pen = new Pen(Color.Black);

                if (arc.IsStrightLine)
                {
                    g.DrawLine(pen, arc.StartPoint.X + offset.Width, arc.StartPoint.Y + offset.Height, (float)arc.EndPoint.X, (float)arc.EndPoint.Y);
                }
                else
                {
                    g.DrawArc(pen, new System.Drawing.RectangleF((float)(arc.CenterPoint.X - arc.Radius) + offset.Width, (float)(arc.CenterPoint.Y - arc.Radius) + offset.Height, (float)(arc.Radius * 2), (float)(arc.Radius * 2)),
                    (float)(arc.StartAngle * 180 / Math.PI),
                    (float)(arc.Angle * 180 / Math.PI));
                }
                if (showLabels)
                {
                    System.Drawing.Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 6F, System.Drawing.FontStyle.Regular);
                    g.DrawString(arcLabel.ToString(), font, System.Drawing.Brushes.White, new System.Drawing.PointF((float)arc.MiddlePoint.X - 5 + offset.Width, (float)arc.MiddlePoint.Y - 5 + offset.Height));
                }
            }
            catch
            {
                //throw;
            }
        }


        private Point GetMaxSizeToDraw()
        {
            float maxWidth = 0, maxHeigth = 0;
            foreach (Arc arc in Arcs)
            {
                if (arc.EndPoint.X > maxWidth)
                    maxWidth = arc.EndPoint.X;

                if (arc.EndPoint.Y > maxHeigth)
                    maxHeigth = arc.EndPoint.Y;

                if (arc.MiddlePoint.X > maxWidth)
                    maxWidth = arc.MiddlePoint.X;

                if (arc.MiddlePoint.Y > maxHeigth)
                    maxHeigth = arc.MiddlePoint.Y;

                if (arc.StartPoint.X > maxWidth)
                    maxWidth = arc.StartPoint.X;

                if (arc.StartPoint.Y > maxHeigth)
                    maxHeigth = arc.StartPoint.Y;
            }
            return new Point(maxWidth, maxHeigth);
        }


        private Point GetMinSizeToDraw()
        {
            float minWidth = float.MaxValue, minHeigth = float.MaxValue;
            foreach (Arc arc in Arcs)
            {
                if (arc.EndPoint.X < minWidth)
                    minWidth = arc.EndPoint.X;

                if (arc.EndPoint.Y < minHeigth)
                    minHeigth = arc.EndPoint.Y;

                if (arc.MiddlePoint.X < minWidth)
                    minWidth = arc.MiddlePoint.X;

                if (arc.MiddlePoint.Y < minHeigth)
                    minHeigth = arc.MiddlePoint.Y;

                if (arc.StartPoint.X < minWidth)
                    minWidth = arc.StartPoint.X;

                if (arc.StartPoint.Y < minHeigth)
                    minHeigth = arc.StartPoint.Y;
            }
            return new Point(minWidth, minHeigth);
        }


        public void Dispose()
        {
            if (uimg != null)
                uimg.Dispose();
            uimg = null;
        }
    }

}
