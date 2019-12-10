using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimilarImageSearch.Engine;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge.Imaging;
using SimilarImageSearch.Engine.Arcs.Optimization;
using SimilarImageSearch.Engine.Arcs;


namespace SimilarImageSearch.Test
{
    [TestClass]
    public class ArcTest
    {
        [TestMethod]
        public void SimpleImageTest()
        {
            string pic = @"C:\Users\solaris\Desktop\pic test\testShape.bmp";
            //string pic = @"G:\b1+sk.bmp";
            Bitmap bmp = (Bitmap)Bitmap.FromFile(pic);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            //byte[] data = new byte[bmpData.Height * bmpData.Width];
            //System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, data, 0, data.Length);
            Shape shape = new Shape(UnmanagedImage.FromManagedImage(bmpData));
            shape.NeighborArcFitCount = 1;
            shape.AnalyzeArcs();
            bmp.UnlockBits(bmpData);
            ArcsView viewer = new ArcsView(shape.Arcs, bmp);
            viewer.ShowDialog();

            shape.OptimizeArcs(new RemoveSmallArcs(10));
            MergeArcs merger = new MergeArcs(MaxAngleDifference: Math.PI/4, MaxDeflectionDifference: 0.05, MaxPointsDistance: 6);
            shape.OptimizeArcs(merger);

            //foreach (ArcCollection arcs in merger.OptimizeArcs(shape.Arcs))
            //{
            //    viewer = new ArcsView(arcs, bmp);
            //    viewer.ShowDialog();
            //}

            viewer = new ArcsView(shape.Arcs, bmp);
            viewer.ShowDialog();

            shape.OptimizeArcs(new MergeClosePoints());
            viewer = new ArcsView(shape.Arcs, bmp);
            viewer.ShowDialog();

            shape.Dispose();
            bmp.Dispose();
        }


        [TestMethod]
        public void TestArcCalculations()
        {
            Arc arc1 = new Arc(new AForge.IntPoint(30, 30), 0, 0.05F);
            arc1.ArcLength = 15;
            Arc arc2 = new Arc(arc1.StartPoint, GeometryTools.CenterPoint(arc1.StartPoint, arc1.EndPoint), arc1.EndPoint);
            ArcCollection col=new ArcCollection();
            col.Add(arc1);
            col.Add(arc2);
            ArcsView viewer = new ArcsView(col, null);
            viewer.ShowDialog();
        }
    }
}
