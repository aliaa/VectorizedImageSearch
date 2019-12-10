using System;
using System.Collections.Generic;
using System.Text;

using AForge.Imaging;
using SimilarImageSearch.Engine.Arcs.Optimization;
using AForge.Imaging.Filters;
using AForge.Imaging.IPPrototyper;
using SimilarImageSearch.Engine;

namespace SimilarImageSearch.OCR
{
    public class OCR : IImageProcessingRoutine
    {
        ReverseImageSearchEngine engine = new ReverseImageSearchEngine();
        SimilarImageSearch.Engine.DBDataSetTableAdapters.ImagesTableAdapter imagesTableAdapter = new Engine.DBDataSetTableAdapters.ImagesTableAdapter();

        public string Name
        {
            get { return "OCR"; }
        }


        public void Process(System.Drawing.Bitmap image, IImageProcessingLog log)
        {
            Process(UnmanagedImage.FromManagedImage(image), true, log);
        }


        public Shape Process(System.Drawing.Bitmap image)
        {
            return Process(UnmanagedImage.FromManagedImage(image), false, null);
        }


        public Shape Process(UnmanagedImage uimg, bool fillLog, IImageProcessingLog log)
        {
            uimg = Grayscale.CommonAlgorithms.RMY.Apply(uimg);
            if (fillLog)
                log.AddImage("Grayscale", uimg.ToManagedImage());

            (new Threshold()).ApplyInPlace(uimg);
            if (fillLog)
                log.AddImage("Threshold", uimg.ToManagedImage());

            ExtractBiggestBlob blobExtractor = new ExtractBiggestBlob();
            uimg = UnmanagedImage.FromManagedImage(blobExtractor.Apply(uimg.ToManagedImage()));
            if (fillLog)
                log.AddImage("Extract biggest blob", uimg.ToManagedImage());

            (new SimpleSkeletonization()).ApplyInPlace(uimg);
            if (fillLog)
                log.AddImage("Skeletonization", uimg.ToManagedImage());

            Shape shape = new Shape(uimg);
            shape.AnalyzeArcs();
            if (fillLog)
                log.AddImage("fitting shape to arcs", shape.DrawArcsToImage(false));

            shape.OptimizeArcs(new RemoveSmallArcs(10));
            if (fillLog)
                log.AddImage("remove small arcs", shape.DrawArcsToImage(false));

            int sizeFactor = Math.Max(shape.Width, shape.Height);
            shape.OptimizeArcs(new MakeSmallArcsStraight(sizeFactor / 15));
            if (fillLog)
                log.AddImage("Make small arcs straight", shape.DrawArcsToImage(false));

            MergeArcs arcsMerger = new MergeArcs(MaxAngleDifference: Math.PI / 6, MaxDeflectionDifference: 0.1, MaxPointsDistance: sizeFactor / 80);
            MergeClosePoints pointsMerger = new MergeClosePoints(maxDistanceToMerge: sizeFactor / 20);

            int arcCount;
            int arcCountAfterOptimization;
            int i = 1;
            while (true)
            {
                arcCount = shape.Arcs.Count;
                shape.OptimizeArcs(arcsMerger);
                if (fillLog)
                    log.AddImage(i.ToString() + "- merge arcs (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

                shape.OptimizeArcs(pointsMerger);
                if (fillLog)
                    log.AddImage(i.ToString() + "- merge close points (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

                arcCountAfterOptimization = shape.Arcs.Count;
                i++;

                if (arcCountAfterOptimization >= arcCount)
                    break;
            }

            shape.RelativateArcsWithMergingPoints();
            if (fillLog)
                log.AddImage("shape after relativation (" + shape.Arcs.Count.ToString() + ")", shape.DrawArcsToImage(false));

            return shape;
        }


        public bool InsertToDatabase(System.Drawing.Bitmap alphabetShape, char alphabet, string imageID)
        {
            return InsertToDatabase(UnmanagedImage.FromManagedImage(alphabetShape), alphabet, imageID);
        }


        public bool InsertToDatabase(UnmanagedImage alphabetShape, char alphabet, string imageID)
        {
            Shape shape = Process(alphabetShape, false, null);
            byte[] emptyMD5 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
            imagesTableAdapter.Insert(imageID, 0, 0, 0, emptyMD5);
            int id = (int)imagesTableAdapter.GetIDByPath(imageID);
            try
            {
                if (engine.InsertToDatabase(shape, id, alphabet - 'A', alphabet.ToString()) == false)
                {
                    imagesTableAdapter.Delete(id);
                    return false;
                }
            }
            catch
            {
                imagesTableAdapter.Delete(id);
                //throw;
            }

            return true;
        }


        public int DeleteAllData()
        {
            return engine.DeleteAllData();
        }


        public string[] Search(System.Drawing.Bitmap alphabetShape, float minAcceptableSimilarity)
        {
            List<string> tags = new List<string>();
            foreach (ShapeInfo inf in engine.SearchSimilarImages(alphabetShape, minAcceptableSimilarity))
            {
                tags.Add(inf.Tags);
            }

            return tags.ToArray();
        }
    }
}
