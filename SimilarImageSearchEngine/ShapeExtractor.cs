using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace SimilarImageSearch.Engine
{

    public static class ShapeExtractor
    {
        const float UpEdgeThresholdPercent = 0.8F;

        public static UnmanagedImage[] ExtractFromImage(UnmanagedImage uimg)
        {
            return ExtractFromImage(false, uimg, Shape.MinimumAcceptableShapeSize);
        }

        public static UnmanagedImage PrepareImageForExtratctingShapes(UnmanagedImage uimg)
        {
            //BitmapData bmpData = bmp.LockBits(new Rectangle(new Point(0, 0), bmp.Size), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            
            uimg = Grayscale.CommonAlgorithms.RMY.Apply(uimg);
            (new SobelEdgeDetector()).ApplyInPlace(uimg);
            (new Threshold()).ApplyInPlace(uimg);
            //(new SimpleSkeletonization()).ApplyInPlace(uimg);

            return uimg;
        }

        public static UnmanagedImage[] ExtractFromImage(bool isPrepared, UnmanagedImage uimg, Size MinimumAcceptableShapeSize)
        {
            if (!isPrepared)
                PrepareImageForExtratctingShapes(uimg);

            #region old code unsafe
            //unsafe
            //{
            //    byte* basePtr = (byte*)uimg.ImageData.ToPointer();
            //    byte* ptr;
            //    int[] horizonalHistogram = new int[uimg.Width];
            //    int[] verticalHistogram = new int[uimg.Height];
            //    int horizonalUpEdgeThreshold = (int)(uimg.Height * UpEdgeThresholdPercent);
            //    int verticalUpEdgeThreshold = (int)(uimg.Width * UpEdgeThresholdPercent);
            //    for (int i = 0; i < uimg.Height; i++)
            //    {
            //        for (int j = 0; j < uimg.Width; j++)
            //        {
            //            ptr = basePtr + (i * uimg.Stride + j)*3;
            //            if (*ptr == 255)
            //            {
            //                horizonalHistogram[j]++;
            //                verticalHistogram[i]++;
            //            }
            //        }
            //    }

            //    //find cut edges
            //    bool s = false;
            //    List<int> hEdges=new List<int>();
            //    for (int i = 0; i < horizonalHistogram.Length; i++)
            //    {
            //        if (horizonalHistogram[i] > 0 && horizonalHistogram[i] < horizonalUpEdgeThreshold && !s)
            //        {
            //            hEdges.Add(i);
            //            s = true;
            //        }
            //        if ((horizonalHistogram[i] == 0 || horizonalHistogram[i] >= horizonalUpEdgeThreshold) && s)
            //        {
            //            hEdges.Add(i - i);
            //            s = false;
            //        }
            //    }
            //    if (s)
            //        hEdges.Add(horizonalHistogram.Length - 1);

            //    s = false;
            //    List<int> vEdges = new List<int>();
            //    for (int i = 0; i < verticalHistogram.Length; i++)
            //    {
            //        if (verticalHistogram[i] > 0 && verticalHistogram[i] < verticalUpEdgeThreshold && !s)
            //        {
            //            vEdges.Add(i);
            //            s = true;
            //        }
            //        if ((verticalHistogram[i] == 0 || verticalHistogram[i] >= verticalUpEdgeThreshold) && s)
            //        {
            //            vEdges.Add(i - i);
            //            s = false;
            //        }
            //    }
            //    if (s)
            //        vEdges.Add(verticalHistogram.Length - 1);

            //    //check cut edges size
            //    int hMinValidSize = Shape.LargeFingerprintSize.Width;
            //    for (int i = 0; i < hEdges.Count; i+=2)
            //    {
            //        if (hEdges[i + 1] - hEdges[i] < hMinValidSize)
            //        {
            //            hEdges.Remove(i);
            //            hEdges.Remove(i);
            //            i -= 2;
            //        }
            //    }
            //    int vMinValidSize = Shape.LargeFingerprintSize.Height;
            //    for (int i = 0; i < vEdges.Count; i += 2)
            //    {
            //        if (vEdges[i + 1] - vEdges[i] < vMinValidSize)
            //        {
            //            vEdges.Remove(i);
            //            vEdges.Remove(i);
            //            i -= 2;
            //        }
            //    }

            //    if (hEdges.Count == 0 || vEdges.Count == 0)
            //        return null;


            //}
            #endregion

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinHeight = MinimumAcceptableShapeSize.Height;
            blobCounter.MinWidth = MinimumAcceptableShapeSize.Width;
            blobCounter.CoupledSizeFiltering = false;
            blobCounter.FilterBlobs = true;
            blobCounter.ProcessImage(uimg);
            Blob[] blobs = blobCounter.GetObjects(uimg, false);
            SimpleSkeletonization skeletonization = new SimpleSkeletonization();
            List<UnmanagedImage> shapes = new List<UnmanagedImage>();
            foreach (Blob blob in blobs)
            {
                skeletonization.ApplyInPlace(blob.Image);
                shapes.Add(blob.Image);
            }

            uimg.Dispose();
            return shapes.ToArray();
        }
    }
}
