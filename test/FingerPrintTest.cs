using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.IO;
using SimilarImageSearchEngine;
using SimilarImageSearchEngine.FingerPrintProcessor;

namespace test
{
    [TestClass]
    public class UnitTest
    {
        const string image1Path = @"C:\Users\solaris\Desktop\pic test\Lamborghini-1600x1200-01.jpg";
        const string image2Path = @"C:\Users\solaris\Desktop\pic test\Lamborghini-1600x1200-02.jpg";

        //const string image1Path = @"C:\Users\solaris\Desktop\pic test\Lamborghini 800x600-07.jpg";
        //const string image2Path = @"C:\Users\solaris\Desktop\pic test\Lamborghini-1600x1200-07.jpg";

        //const string image1Path = @"C:\Users\solaris\Desktop\pic test\Lamborghini-1600x1200-07.jpg";
        //const string image2Path = @"C:\Users\solaris\Desktop\pic test\Lamborghini 800x600-07.jpg";

        //const string image1Path = @"C:\Users\solaris\Desktop\FilterTest\4.jpg";

        const string resultPath = @"G:\FP";

        [TestMethod]
        public void TestHoleProcess()
        {
            AForge.Imaging.UnmanagedImage[] uimges = ShapeExtractor.ExtractFromImage(Image.FromFile(image1Path), FingerprintShape.MinimumAcceptableShapeSize);
            FingerprintShape[] shapes = new FingerprintShape[uimges.Length];
            for (int i = 0; i < uimges.Length; i++)
            {
                shapes[i] = new FingerprintShape(uimges[i]);
            }

            int j = 1;
            foreach (FingerprintShape sh in shapes)
            {
                sh.MakeFingerprints();
                sh.LargeFingerPrintImage.Save(Path.Combine(resultPath, j.ToString() + " L.jpeg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                sh.MediumFingerPrintImage.Save(Path.Combine(resultPath, j.ToString() + " M.jpeg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                sh.SmallFingerPrintImage.Save(Path.Combine(resultPath, j.ToString() + " S.jpeg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                j++;
            }
        }

        [TestMethod]
        public void TestFingerPrintProcessor()
        {
            AForge.Imaging.UnmanagedImage[] uimages1 = ShapeExtractor.ExtractFromImage(Image.FromFile(image1Path), FingerprintShape.MinimumAcceptableShapeSize);
            AForge.Imaging.UnmanagedImage[] uimages2 = ShapeExtractor.ExtractFromImage(Image.FromFile(image2Path), FingerprintShape.MinimumAcceptableShapeSize);
            FingerprintShape[] img1Shapes = new FingerprintShape[uimages1.Length];
            FingerprintShape[] img2Shapes = new FingerprintShape[uimages2.Length];
            for (int i = 0; i < uimages1.Length; i++)
            {
                img1Shapes[i] = new FingerprintShape(uimages1[i]);
            }
            for (int i = 0; i < uimages2.Length; i++)
            {
                img2Shapes[i] = new FingerprintShape(uimages2[i]);
            }

            int onesS1 = FingerPrintProcessor.OneCount(img1Shapes[0].SmallFingerPrint);
            int onesM1 = FingerPrintProcessor.OneCount(img1Shapes[0].MediumFingerPrint);
            int onesL1 = FingerPrintProcessor.OneCount(img1Shapes[0].LargeFingerPrint);

            int onesS2 = FingerPrintProcessor.OneCount(img2Shapes[0].SmallFingerPrint);
            int onesM2 = FingerPrintProcessor.OneCount(img2Shapes[0].MediumFingerPrint);
            int onesL2 = FingerPrintProcessor.OneCount(img2Shapes[0].LargeFingerPrint);

            int resS = FingerPrintProcessor.CompareFingerPrints(
                FingerPrintProcessor.DecodeFingerPrint(img1Shapes[0].SmallFingerPrint, FingerprintShape.SmallFingerprintSize.Width, FingerprintShape.SmallFingerprintSize.Height),
                FingerPrintProcessor.DecodeFingerPrint(img2Shapes[0].SmallFingerPrint, FingerprintShape.SmallFingerprintSize.Width, FingerprintShape.SmallFingerprintSize.Height),
                5, 5, (onesS1+onesS2)/2);

            int resM = FingerPrintProcessor.CompareFingerPrints(
                FingerPrintProcessor.DecodeFingerPrint(img1Shapes[0].MediumFingerPrint, FingerprintShape.MediumFingerprintSize.Width, FingerprintShape.MediumFingerprintSize.Height),
                FingerPrintProcessor.DecodeFingerPrint(img2Shapes[0].MediumFingerPrint, FingerprintShape.MediumFingerprintSize.Width, FingerprintShape.MediumFingerprintSize.Height),
                12, 12, (onesM1+onesM2)/2);

            int resL = FingerPrintProcessor.CompareFingerPrints(
                FingerPrintProcessor.DecodeFingerPrint(img1Shapes[0].LargeFingerPrint, FingerprintShape.LargeFingerprintSize.Width, FingerprintShape.LargeFingerprintSize.Height),
                FingerPrintProcessor.DecodeFingerPrint(img2Shapes[0].LargeFingerPrint, FingerprintShape.LargeFingerprintSize.Width, FingerprintShape.LargeFingerprintSize.Height),
                20, 20, (onesL1+onesL2)/2);
        }
    }
}
