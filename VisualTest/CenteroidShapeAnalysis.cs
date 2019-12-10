using AForge.Imaging.IPPrototyper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using AForge.Imaging;
using AForge.Imaging.Filters;
using CenteroidSimilarity;
using AForge;
using AForge.Math;

namespace SimilarImageSearch.VisualTest
{
    public class CenteroidShapeAnalysis : IImageProcessingRoutine
    {
        private readonly Size MinimumAcceptableShapeSize = new Size(10, 10);

        public string Name
        {
            get
            {
                return "Centeroid Shape Analysis";
            }
        }

        public void Process(Bitmap image, IImageProcessingLog log)
        {
            UnmanagedImage uimg = UnmanagedImage.FromManagedImage(image);
            uimg = Grayscale.CommonAlgorithms.RMY.Apply(uimg);
            log.AddImage("Grayscale", uimg.ToManagedImage());
            
            (new Threshold()).ApplyInPlace(uimg);
            log.AddImage("Threshold", uimg.ToManagedImage());
            (new FillHoles()).ApplyInPlace(uimg);
            log.AddImage("Fill Holes", uimg.ToManagedImage());

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinHeight = MinimumAcceptableShapeSize.Height;
            blobCounter.MinWidth = MinimumAcceptableShapeSize.Width;
            blobCounter.CoupledSizeFiltering = false;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Area;
            blobCounter.ProcessImage(uimg);
            Blob[] blobs = blobCounter.GetObjects(uimg, true);
            if (blobs.Length == 0)
                return;
            Blob blob = blobs[0];
            log.AddImage("Biggest blob extracted", blob.Image.ToManagedImage());

            (new SobelEdgeDetector()).ApplyInPlace(blob.Image);
            log.AddImage("Edge Detector", blob.Image.ToManagedImage());

            (new SimpleSkeletonization()).ApplyInPlace(blob.Image);
            uimg.Dispose();
            uimg = blob.Image.Clone();

            UnmanagedImage uimgColored = CopyUimgColored(uimg);
            AForge.Point center = blob.CenterOfGravity;
            Drawing.FillRectangle(uimgColored, 
                new Rectangle((int)center.X-1, (int)center.Y-1, 3, 3), Color.Green);
            log.AddImage("Skeletonized", uimgColored.ToManagedImage());

            List<IntPoint> pointsCloud = CenteroidAnalysis.GetPointsCloud(uimg);
            List<Tuple<double, int>> polar = CenteroidAnalysis.GetPointsCloudAnglesAndDistanceFromCenter(pointsCloud, center);
            int[] values = new int[polar.Count];
            for (int i = 0; i < polar.Count; i++)
                values[i] = polar[i].Item2;
            Statistic stat = new Statistic(values);
            double mean = stat.Mean;
            double stdDev = stat.StdDev;
            log.AddMessage("stdDev= " + stdDev);
            log.AddMessage("stdDev/mean= " + stdDev/mean);
            Bitmap imgColored = uimgColored.ToManagedImage();
            using (Graphics g = Graphics.FromImage(imgColored))
            {
                g.DrawEllipse(new Pen(Color.Orange), (float)(center.X - mean), (float)(center.Y - mean), (float)(mean * 2), (float)(mean * 2));
                g.Save();
            }
            log.AddImage("Mean circle", imgColored);

            log.AddImage("Graph", CreateGraph(polar, stat).ToManagedImage());

            double[] meanedData = MeansOfDegrees(polar, 256);
            //double[] meanedData = ClusteredMeans(polar, 256);
            UnmanagedImage recreatedUimg = RecreateImageFromPolarData(meanedData, center, uimg.Width, uimg.Height);
            log.AddImage("Average points", recreatedUimg.ToManagedImage());

            Complex[] complexData = new Complex[meanedData.Length];
            for (int i = 0; i < meanedData.Length; i++)
                complexData[i] = new Complex(meanedData[i], 0);
            FourierTransform.DFT(complexData, FourierTransform.Direction.Forward);
            Complex[] ftCeos = (Complex[])complexData.Clone();
            FourierTransform.DFT(complexData, FourierTransform.Direction.Backward);
            double[] data = new double[complexData.Length];
            for (int i = 0; i < data.Length; i++)
                data[i] = complexData[i].Re;
            recreatedUimg.Dispose();
            recreatedUimg = RecreateImageFromPolarData(data, center, uimg.Width, uimg.Height);
            log.AddImage("Recreated After DFT", recreatedUimg.ToManagedImage());

            Complex[] ftCasted = new Complex[100];
            for (int i = 0; i < ftCasted.Length; i++)
                ftCasted[i] = ftCeos[i];
            FourierTransform.DFT(ftCasted, FourierTransform.Direction.Backward);
            double[] castedData = new double[ftCasted.Length];
            for (int i = 0; i < ftCasted.Length; i++)
                castedData[i] = ftCasted[i].Re;
            recreatedUimg.Dispose();
            recreatedUimg = RecreateImageFromPolarData(castedData, center, uimg.Width, uimg.Height);
            log.AddImage("Recreated After DFT casted (" + castedData.Length + ")", recreatedUimg.ToManagedImage());
            
            //double[] ceofficients = CenteroidAnalysis.DFT(meansDegs, meansDegs.Length, CenteroidAnalysis.Direction.Forward);
            //double[] regeneratedAfterDFT = CenteroidAnalysis.DFT(ceofficients, ceofficients.Length, CenteroidAnalysis.Direction.Backward);
        }
        
        private UnmanagedImage CopyUimgColored(UnmanagedImage source)
        {
            UnmanagedImage dest = UnmanagedImage.Create(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            for (int i = 0; i < source.Height; i++)
                for (int j = 0; j < source.Width; j++)
                    dest.SetPixel(j, i, source.GetPixel(j, i));
            return dest;
        }

        private UnmanagedImage CreateGraph(List<Tuple<double, int>> polar, Statistic stats)
        {
            UnmanagedImage graph = UnmanagedImage.Create(380, 220, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Drawing.Line(graph, new IntPoint(10, 10), new IntPoint(10, 210), Color.Orange);
            Drawing.Line(graph, new IntPoint(10, 110), new IntPoint(370, 110), Color.Orange);

            double rangeScale = 100.0 / Math.Max(stats.Max - stats.Mean, stats.Mean - stats.Min);
            foreach (var tuple in polar)
            {
                double deg = AngleRadToDeg(tuple.Item1);
                double y = (tuple.Item2 - stats.Mean) * rangeScale ;
                Drawing.FillRectangle(graph, new Rectangle((int)deg + 10, (int)Math.Round(110 - y), 1, 1), Color.White);
            }
            return graph;
        }

        private double AngleRadToDeg(double rad)
        {
            double deg = rad * 180 / Math.PI;
            if (deg < 0)
                deg += 360;
            return deg;
        }

        private double[] MeansOfDegrees(List<Tuple<double, int>> polar, int count)
        {
            double[] means = new double[count];
            double step = Math.PI * 2 / count;
            for (int i = 0; i < count; i++)
            {
                double deg = -Math.PI + i * step;
                means[i] = GetMeanBetweenKeys(polar, deg, deg + step);
            }
            return means;
        }

        private double[] ClusteredMeans(List<Tuple<double, int>> polar, int count)
        {
            if (polar.Count < count)
                throw new Exception("polar size can not be smaller than count");
            double[] output = new double[count];
            for (int i = 0; i < count; i++)
            {
                int lowIndex = (int)Math.Floor((float)i * polar.Count / count);
                int highIndex = (int)Math.Ceiling((float)(i + 1) * polar.Count / count);
                if (highIndex >= polar.Count)
                    highIndex = polar.Count - 1;

                int sum = 0;
                for (int j = lowIndex; j <= highIndex; j++)
                    sum += polar[j].Item2;
                output[i] = (double)sum / (highIndex - lowIndex + 1);
            }
            return output;
        }

        private double GetMeanBetweenKeys(List<Tuple<double, int>> polar, double start, double end)
        {
            List<int> values = new List<int>();
            foreach (var tuple in polar)
            {
                if (tuple.Item1 >= start && tuple.Item1 < end)
                {
                    values.Add(tuple.Item2);
                }
            }
            if (values.Count == 0)
                return 0;
            return values.Average();
        }

        private UnmanagedImage RecreateImageFromPolarData(double[] data, AForge.Point center, int width, int height)
        {
            UnmanagedImage uimg = UnmanagedImage.Create(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            double step = 2 * Math.PI / data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                double x = data[i] * Math.Cos(step * i - Math.PI) + center.X;
                double y = data[i] * Math.Sin(step * i - Math.PI) + center.Y;
                uimg.SetPixel((int)Math.Round(x), (int)Math.Round(y), 255);
            }
            return uimg;
        }
    }
}
