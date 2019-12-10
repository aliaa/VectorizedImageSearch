using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SimilarImageSearch.Test
{
    [TestClass]
    public class OCRTest
    {
        OCR.OCR ocr = new OCR.OCR();

        [TestMethod]
        public void InsertToDatabase()
        {
            ocr.DeleteAllData();
            foreach (string file in Directory.GetFiles(@"..\..\..\Test\Alphabets"))
            {
                char alphabet = Path.GetFileName(file)[0];
                ocr.InsertToDatabase((System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(file), alphabet, "Arial");
            }
        }
    }
}
