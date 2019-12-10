using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Imaging.IPPrototyper;

namespace SimilarImageSearch.VisualTest
{
    public class InversionSkeletonization : IImageProcessingRoutine
    {

        public string Name
        {
            get { return "Inversion Skeletonization"; }
        }

        public void Process(System.Drawing.Bitmap image, IImageProcessingLog log)
        {
            UnmanagedImage uimg = UnmanagedImage.FromManagedImage(image);
            uimg = Grayscale.CommonAlgorithms.RMY.Apply(uimg);
            log.AddImage("Grayscale", uimg.ToManagedImage());

            (new Threshold()).ApplyInPlace(uimg);
            log.AddImage("Threshold", uimg.ToManagedImage());

            Invert colorInverter = new Invert();
            colorInverter.ApplyInPlace(uimg);
            log.AddImage("Inverted image", uimg.ToManagedImage());

            SimpleSkeletonization skeletonizer = new SimpleSkeletonization();
            skeletonizer.ApplyInPlace(uimg);
            log.AddImage("Skeletonized image", uimg.ToManagedImage());

            ArcVectorization vectorizer = new ArcVectorization();
            vectorizer.Process(uimg.ToManagedImage(), log);
        }
    }
}
