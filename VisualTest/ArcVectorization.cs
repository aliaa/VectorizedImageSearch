using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Imaging.IPPrototyper;
using System.Drawing;
using SimilarImageSearch.Engine;
using SimilarImageSearch.Engine.Arcs.Optimization;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace SimilarImageSearch.VisualTest
{
    public class ArcVectorization : IImageProcessingRoutine
    {

        public string Name
        {
            get { return "Arc Vectorization"; }
        }

        public void Process(Bitmap image, IImageProcessingLog log)
        {
            Process(image, log, string.Empty);
        }

        public void Process(Bitmap image, IImageProcessingLog log, string shapeID)
        {
            UnmanagedImage uimg = UnmanagedImage.FromManagedImage(image);
            if(uimg.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                uimg = Grayscale.CommonAlgorithms.Y.Apply(uimg);
            Shape shape = new Shape(uimg);
            //TestUimg(shape, shapeID);
            shape.AnalyzeArcs(NeighborArcFitCount: 1);
            //image.Save("G:\\" + shapeID + "-image.bmp");
            log.AddImage(shapeID + " -fitting image to arcs (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

            shape.OptimizeArcs(new RemoveSmallArcs(5));
            log.AddImage(shapeID + " -remove small arcs (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

            int sizeFactor = Math.Max(shape.Width, shape.Height);
            shape.OptimizeArcs(new MakeSmallArcsStraight(sizeFactor / 15));
            log.AddImage(shapeID + " -make small arcs straight (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

            MergeArcs arcsMerger = new MergeArcs(MaxAngleDifference: Math.PI / 6, MaxDeflectionDifference: 0.1, MaxPointsDistance: sizeFactor / 80);
            MergeClosePoints pointsMerger = new MergeClosePoints(maxDistanceToMerge: sizeFactor / 40);

            int arcCount;
            int arcCountAfterOptimization;
            int i = 1;
            while (true)
            {
                arcCount = shape.Arcs.Count;
                shape.OptimizeArcs(arcsMerger);
                log.AddImage(shapeID + " -[" + i.ToString() + "] merge arcs (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

                shape.OptimizeArcs(pointsMerger);
                log.AddImage(shapeID + " -[" + i.ToString() + "] merge close points (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

                arcCountAfterOptimization = shape.Arcs.Count;
                i++;

                if (arcCountAfterOptimization >= arcCount)
                    break;
            }
            //shape.OptimizeArcs(new RemoveSmallArcs(pointsMerger.MaxDistanceToMerge*4));
            //log.AddImage(shapeID + "  -[" + i.ToString() + "] remove small arcs 2 (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

            shape.RelativateArcsWithMergingPoints();
            shape.OptimizeArcs(arcsMerger);
            log.AddImage(shapeID + " -shape after relativation (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));
        }


        private void TestUimg(Shape shape, string shapeID)
        {
            Bitmap bmp = new Bitmap(shape.Width, shape.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            byte[] data = new byte[bmp.Height * bmp.Width];
            for (int y = 0; y < shape.Height; y++)
            {
                for (int x = 0; x < shape.Width; x++)
                {
                    data[y * bmp.Width + x] = shape.GetDataFromUimg(x, y);
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            bmp.UnlockBits(bmpData);
            bmp.Save("G:\\" + shapeID + ".bmp");
        }
    }
}
