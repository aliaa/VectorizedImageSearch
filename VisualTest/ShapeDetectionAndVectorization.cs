using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Imaging.IPPrototyper;
using SimilarImageSearch.Engine;
using AForge.Imaging;
using SimilarImageSearch.Engine.Arcs.Optimization;
using AForge.Imaging.Filters;

namespace SimilarImageSearch.VisualTest
{
    public class ShapeDetectionAndVectorization : IImageProcessingRoutine
    {
        ArcVectorization vectorizer = new ArcVectorization();

        public string Name
        {
            get { return "Shape Detection And Vectorization"; }
        }

        public void Process(System.Drawing.Bitmap image, IImageProcessingLog log)
        {
            UnmanagedImage uimg = UnmanagedImage.FromManagedImage(image);
            uimg = Grayscale.CommonAlgorithms.RMY.Apply(uimg);
            log.AddImage("Grayscale", uimg.ToManagedImage());
            (new SobelEdgeDetector()).ApplyInPlace(uimg);
            log.AddImage("Edge Detector", uimg.ToManagedImage());
            (new Threshold()).ApplyInPlace(uimg);
            log.AddImage("Threshold", uimg.ToManagedImage());

            Shape.MinimumAcceptableShapeSize = new System.Drawing.Size(50,50);
            AForge.Imaging.UnmanagedImage[] uimages = ShapeExtractor.ExtractFromImage(true, uimg, Shape.MinimumAcceptableShapeSize);
            Shape[] shapes = new Shape[uimages.Length];
            for (int i = 0; i < uimages.Length; i++)
            {
                shapes[i] = new Shape(uimages[i]);
            }
            for (int i = 0; i < shapes.Length; i++)
            {
                string shapeID = "skeletonized Shape #" + i.ToString();
                log.AddImage(shapeID, shapes[i].Image);
                vectorizer.Process(shapes[i].Image, log, shapeID);
            }
        }
    }
}
