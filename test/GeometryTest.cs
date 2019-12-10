using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimilarImageSearch.Engine;
using System.Diagnostics;
using AForge;

namespace SimilarImageSearch.Test
{
    [TestClass]
    public class GeometryTest
    {
        [TestMethod]
        public void TestCircleCenterCalculation()
        {
            double a = Math.Atan2(1, 2);
            double b = Math.Atan2(-1, -2);
            double c = Math.Atan2(0, 2);
            double d = Math.Atan2(2, 0);



            Point p1 = new Point(10, 10);
            Point p2 = new Point(20, 10);
            Point p3 = new Point(10, 20);

            Point res = GeometryTools.CircleCenter(p1, p2, p3);
            double radius = GeometryTools.Distance(p1, res);
            Debug.WriteLine(res);
        }
    }
}
