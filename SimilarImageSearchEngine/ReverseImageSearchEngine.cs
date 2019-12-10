using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using AForge.Imaging.Filters;
using SimilarImageSearch.Engine.Arcs.Optimization;
using System.Security.Cryptography;
using SimilarImageSearch.Engine.Arcs;
using AForge;

namespace SimilarImageSearch.Engine
{
    public class ReverseImageSearchEngine : IDisposable
    {
        DBDataSetTableAdapters.ImagesTableAdapter imagesTableAdapter;
        DBDataSetTableAdapters.ShapesTableAdapter shapesTableAdapter;
        DBDataSetTableAdapters.ArcsTableAdapter arcsTableAdapter;
        DBDataSetTableAdapters.ArcRelationsTableAdapter arcRelationsTableAdapter;
        MD5 md5Hasher = new MD5CryptoServiceProvider();

        public static readonly string[] ImageFilesExtentioins = new string[]
        {
            ".jpg", ".jpeg" ,".bmp",".gif",".png"
        };

        public readonly System.Data.DataException disInsertedDataException = new System.Data.DataException("Application error on inserting data. No data has been added.");


        object dbLockObj = new object();


        public ReverseImageSearchEngine()
        {
            imagesTableAdapter = new DBDataSetTableAdapters.ImagesTableAdapter();
            shapesTableAdapter = new DBDataSetTableAdapters.ShapesTableAdapter();
            arcsTableAdapter = new DBDataSetTableAdapters.ArcsTableAdapter();
            arcRelationsTableAdapter = new DBDataSetTableAdapters.ArcRelationsTableAdapter();
        }


        public bool InsertToDatabase(string filePath)
        {
            Bitmap bmp;
            byte[] md5Hash;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                bmp = (Bitmap)Bitmap.FromStream(fs);
                fs.Position = 0;
                md5Hash = md5Hasher.ComputeHash(fs);
            }
            Shape[] shapes = AnalyzeImage(bmp);
            FileInfo finfo = new FileInfo(filePath);
            lock (dbLockObj)
            {
                if (imagesTableAdapter.Insert(filePath, (int)finfo.Length, bmp.Width, bmp.Height, md5Hash) == 0)
                    return false;
                int imageID = (int)imagesTableAdapter.GetIDByPath(filePath);

                try
                {
                    for (int i = 0; i < 1 /*shapes.Length*/; i++)  //temporarily inserting only the biggest (first) shape.
                    {
                        if (InsertToDatabase(shapes[i], imageID, i, string.Empty) == false)
                        {
                            imagesTableAdapter.Delete(imageID);
                            return false;
                        }
                    }
                }
                catch
                {
                    imagesTableAdapter.Delete(imageID);
                    throw;
                }
            }

            return true;
        }


        public bool InsertToDatabase(Shape shape, int imageID, int shapeID, string tags)
        {
            if (shapesTableAdapter.Insert(imageID, shapeID, shape.Width, shape.Height, tags) == 0)
            {
                return false;
            }

            for (int j = 0; j < shape.Arcs.Count; j++)
            {
                if (arcsTableAdapter.Insert(imageID, shapeID, j, (float)shape.Arcs[j].Deflection, (float)shape.Arcs[j].ArcLength) == 0)
                {
                    return false;
                }

                foreach (Arc arc in shape.Arcs[j].NextArcsAndAngles.Keys)
                {
                    int arc2Index = shape.Arcs.IndexOf(arc);
                        if (arcRelationsTableAdapter.Insert
                            (imageID, shapeID, j, arc2Index, (float)shape.Arcs[j].NextArcsAndAngles[arc], (byte)ArcRelationType.Chain) == 0)
                            throw disInsertedDataException;
                }
                foreach (Arc arc in shape.Arcs[j].HeadToHeadArcsAndAngles.Keys)
                {
                    int arc2Index = shape.Arcs.IndexOf(arc);
                        if (arcRelationsTableAdapter.Insert
                            (imageID, shapeID, j, arc2Index, (float)shape.Arcs[j].HeadToHeadArcsAndAngles[arc], (byte)ArcRelationType.HeadToHead) == 0)
                            throw disInsertedDataException;
                }
                foreach (Arc arc in shape.Arcs[j].TailToTailArcsAndAngles.Keys)
                {
                    int arc2Index = shape.Arcs.IndexOf(arc);
                        if (arcRelationsTableAdapter.Insert
                            (imageID, shapeID, j, arc2Index, (float)shape.Arcs[j].TailToTailArcsAndAngles[arc], (byte)ArcRelationType.TailToTail) == 0)
                            throw disInsertedDataException;
                }
            }
            return true;
        }


        public bool DeleteFromDatabase(string filePath)
        {
            lock (dbLockObj)
            {
                int imageID = (int)imagesTableAdapter.GetIDByPath(filePath);
                return (imagesTableAdapter.Delete(imageID) != 0);
            }
        }


        public int DeleteAllData()
        {
            return imagesTableAdapter.DeleteAll();
        }


        public static Shape[] AnalyzeImage(Bitmap bmp)
        {
            AForge.Imaging.UnmanagedImage uimg = AForge.Imaging.UnmanagedImage.FromManagedImage(bmp);
            AForge.Imaging.UnmanagedImage[] uimages = ShapeExtractor.ExtractFromImage(false, uimg, Shape.MinimumAcceptableShapeSize);
            List<Shape> shapes = new List<Shape>(uimages.Length);
            for (int i = 0; i < uimages.Length; i++)
            {
                shapes.Add(new Shape(uimages[i]));
            }

            int sizeFactor = Math.Max(shapes[0].Width, shapes[0].Height);
            MergeArcs arcsMerger = new MergeArcs(MaxAngleDifference: Math.PI / 6, MaxDeflectionDifference: 0.1, MaxPointsDistance: sizeFactor / 80);
            MergeClosePoints pointsMerger = new MergeClosePoints(maxDistanceToMerge: sizeFactor / 40);
            RemoveSmallArcs smallArcsRemover = new RemoveSmallArcs(MinArcLength: 5);
            MakeSmallArcsStraight smallArcsStraighter = new MakeSmallArcsStraight(sizeFactor / 10);

            foreach (Shape shape in shapes)
            {
                shape.AnalyzeArcs(NeighborArcFitCount: 1);
                shape.OptimizeArcs(smallArcsRemover);
                shape.OptimizeArcs(smallArcsStraighter);
                int arcCount;
                int arcCountAfterOptimization;
                int i = 1;

                while (true)
                {
                    arcCount = shape.Arcs.Count;
                    shape.OptimizeArcs(arcsMerger);
                    shape.OptimizeArcs(pointsMerger);
                    arcCountAfterOptimization = shape.Arcs.Count;
                    i++;

                    if (arcCountAfterOptimization >= arcCount)
                        break;
                }

                shape.RelativateArcsWithMergingPoints();
            }

            shapes.Sort(new Comparison<Shape>(CompareShapesBySize));
            shapes.Reverse();
            return shapes.ToArray();
        }


        public static int CompareShapesBySize(Shape s1, Shape s2)
        {
            return (s1.Width * s1.Height) - (s2.Width * s2.Height);
        }


        public static IEnumerable<string> SearchImageFiles(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                foreach (string ex in ImageFilesExtentioins)
                {
                    if (Path.GetExtension(file).ToLower() == ex)
                        yield return file;
                }
            }
            foreach (string dir in Directory.GetDirectories(path))
            {
                foreach (string file in SearchImageFiles(dir))
                {
                    yield return file;
                }
            }
        }


        public static IEnumerable<string> SearchImageFiles(string path, int minimumFileSize)
        {
            foreach (string file in SearchImageFiles(path))
            {
                FileInfo info = new FileInfo(file);
                if (info.Length > minimumFileSize)
                    yield return file;
            }
        }


        public IEnumerable<ShapeInfo> SearchSimilarImages(string filePath, float MinAcceptableSimilarity)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filePath);
            byte[] md5Hash;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                md5Hash = md5Hasher.ComputeHash(fs);
            }
            foreach (ShapeInfo item in SearchSimilarImages(bmp, true, md5Hash, MinAcceptableSimilarity))
            {
                yield return item;
            }
        }


        public IEnumerable<ShapeInfo> SearchSimilarImages(Bitmap image, float MinAcceptableSimilarity)
        {
            foreach (ShapeInfo item in SearchSimilarImages(image, false, null, MinAcceptableSimilarity))
            {
                yield return item;
            }
        }


        private IEnumerable<ShapeInfo> SearchSimilarImages(System.Drawing.Bitmap image, bool compareHash, byte[] md5Hash, float MinAcceptableSimilarity)
        {
            Shape[] shapesToSearch = AnalyzeImage(image);
            List<ArcPair>[] shapesArcPairs = new List<ArcPair>[shapesToSearch.Length];
            for (int i = 0; i < 1 /*shapesToSearch.Length */; i++) //temporarily only search for the biggest (first) shape.
            {
                shapesArcPairs[i]= MakeArcPairs(shapesToSearch[i].Arcs, MinAcceptableSimilarity);
            }

            lock (dbLockObj)
            {
                DBDataSet.ShapesDataTable shapesTable = shapesTableAdapter.GetData();
                for (int i = 0; i < shapesTable.Count; i++)
                {
                    int imageID = (int)shapesTable[i]["ImageID"];
                    int shapeID = (int)shapesTable[i]["ShapeID"];

                    DBDataSet.ImagesDataTable imageTable = imagesTableAdapter.GetDataByID(imageID);
                    if (compareHash)
                    {
                        byte[] imageHash = (byte[])imageTable[0]["MD5Hash"];
                        if (CompareBytes(md5Hash, imageHash) == true)
                        {
                            ShapeInfo info = new ShapeInfo(imageID, shapeID,
                                    (string)imageTable[0]["Path"], new Size((int)imageTable[0]["Width"], (int)imageTable[0]["Height"]),
                                    new Size((int)shapesTable[i]["Width"], (int)shapesTable[i]["Height"]), (int)imageTable[0]["FileSize"], 1, (string)shapesTable[i]["Tags"]);
                            yield return info;
                            continue;
                        }
                    }

                    DBDataSet.ArcsDataTable arcsTable = arcsTableAdapter.GetDataByImageAndShapeID(imageID, shapeID);
                    DBDataSet.ArcRelationsDataTable relationsTable = arcRelationsTableAdapter.GetDataByImageAndShapeID(imageID, shapeID);
                    List<ArcPair> pairsInDB = MakeArcPairs(arcsTable, relationsTable, MinAcceptableSimilarity);

                    for (int j = 0; j < 1 /*shapesToSearch.Length */; j++) //temporarily only search for the biggest (first) shape.
                    {
                        double similarity = AnalyzeSimilarity(shapesArcPairs[j], pairsInDB, MinAcceptableSimilarity);

                        if (similarity >= MinAcceptableSimilarity)
                        {
                            ShapeInfo info = new ShapeInfo(imageID, (int)shapesTable[i]["ShapeID"],
                                (string)imageTable[0]["Path"], new Size((int)imageTable[0]["Width"], (int)imageTable[0]["Height"]),
                                new Size((int)shapesTable[i]["Width"], (int)shapesTable[i]["Height"]), (int)imageTable[0]["FileSize"],
                                (float)similarity, (string)shapesTable[i]["Tags"]);

                            yield return info;
                        }
                    }
                }
            }
        }


        private static bool CompareBytes(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length)
                return false;
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                    return false;
            }
            return true;
        }


        private static double CalculatePairSimilarity(ArcPair pair1, ArcPair pair2)
        {
            double angleFactor = 1 - Math.Abs(GeometryTools.AngleDifference(pair1.Angle, pair2.Angle)) / Math.PI;
            double defFactor1 = 1 - (Math.Abs(GeometryTools.AngleDifference(pair1.Arc1Angle, pair2.Arc1Angle)) + Math.Abs(GeometryTools.AngleDifference(pair1.Arc2Angle, pair2.Arc2Angle))) / (2 * Math.PI);
            double defFactor2 = 1 - (Math.Abs(GeometryTools.AngleDifference(pair1.Arc1Angle, pair2.Arc2Angle)) + Math.Abs(GeometryTools.AngleDifference(pair1.Arc1Angle, pair2.Arc2Angle))) / (2 * Math.PI);
            double defFactor = Math.Max(defFactor1, defFactor2);
            double lengthFactor = Math.Min(pair1.LengthesRatio, pair2.LengthesRatio) / Math.Max(pair1.LengthesRatio, pair2.LengthesRatio);

            return Math.Pow(angleFactor,3) * 0.25 + Math.Pow(defFactor,3) * 0.5 + Math.Pow(lengthFactor,3) * 0.25;
        }


        private static List<ArcPair> MakeArcPairs(ArcCollection arcs, float minAcceptableSimilarity)
        {
            List<ArcPair> pairs = new List<ArcPair>();
            foreach (Arc a1 in arcs)
            {
                foreach (Arc a2 in a1.NextArcsAndAngles.Keys)
                {
                    ArcPair p = new ArcPair(a1.ArcLength, a2.ArcLength, a1.NextArcsAndAngles[a2], a1.Angle, a2.Angle, 1);
                    pairs.Add(p);
                }
                foreach (Arc a2 in a1.HeadToHeadArcsAndAngles.Keys)
                {
                    ArcPair p = new ArcPair(a1.ArcLength, a2.ArcLength, a1.HeadToHeadArcsAndAngles[a2], a1.Angle, a2.Angle, 1);
                    pairs.Add(p);
                }
                foreach (Arc a2 in a1.TailToTailArcsAndAngles.Keys)
                {
                    ArcPair p = new ArcPair(a1.ArcLength, a2.ArcLength, a1.TailToTailArcsAndAngles[a2], a1.Angle, a2.Angle, 1);
                    pairs.Add(p);
                }
            }
            MergeSimilarPairs(ref pairs, minAcceptableSimilarity);
            return pairs;
        }


        private static List<ArcPair> MakeArcPairs(DBDataSet.ArcsDataTable arcTable, DBDataSet.ArcRelationsDataTable relationsTable, float minAcceptableSimilarity)
        {
            List<ArcPair> pairs = new List<ArcPair>();
            for (int i = 0; i < relationsTable.Count; i++)
            {
                ArcRelationType rtype = (ArcRelationType)(byte)relationsTable[i]["RelationType"];
                if (rtype != ArcRelationType.Previous)
                {
                    int arc1ID = (int)relationsTable[i]["Arc1ID"];
                    int arc2ID = (int)relationsTable[i]["Arc2ID"];
                    int i1 = FindIndexFromArcTableByID(arcTable, arc1ID);
                    int i2 = FindIndexFromArcTableByID(arcTable, arc2ID);
                    float i1Length = (float)arcTable[i1]["Length"];
                    float i2Length = (float)arcTable[i2]["Length"];
                    double i1angle = Arc.CalculateAngle(i1Length, Arc.CalculateRadius((float)arcTable[i1]["Deflection"]));
                    double i2angle = Arc.CalculateAngle(i2Length, Arc.CalculateRadius((float)arcTable[i2]["Deflection"]));
                    double difAnlge = (float)relationsTable[i]["AngleDifference"];
                    ArcPair p = new ArcPair(i1Length, i2Length, difAnlge, i1angle, i2angle, 1);
                    pairs.Add(p);
                }
            }
            MergeSimilarPairs(ref pairs, minAcceptableSimilarity);
            return pairs;
        }


        private static void MergeSimilarPairs(ref List<ArcPair> pairs, float minAcceptableSimilarity)
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                for (int j = i + 1; j < pairs.Count; j++)
                {
                    if (CalculatePairSimilarity(pairs[i], pairs[j]) > minAcceptableSimilarity)
                    {
                        pairs.RemoveAt(j);
                        j--;
                        pairs[i].Count++;
                    }
                }
            }
        }


        private static int FindIndexFromArcTableByID(DBDataSet.ArcsDataTable arcTable, int arcID)
        {
            for (int i = 0; i < arcTable.Count; i++)
            {
                if ((int)arcTable[i]["ArcID"] == arcID)
                {
                    return i;
                }
            }
            return -1;
        }


        private static double AnalyzeSimilarity(List<ArcPair> pairs1, List<ArcPair> pairs2, float minAcceptableSimilarity)
        {
            double lengthAvgOfPairs1 = CalculatePairsLengthAverage(pairs1);
            double lengthAvgOfPairs2 = CalculatePairsLengthAverage(pairs2);
            int similarityCount = 0;
            double sum = 0;
            int pairs1Count = 0, pairs2Count = 0;
            for (int i = 0; i < pairs1.Count; i++)
                pairs1Count += pairs1[i].Count;
            for (int i = 0; i < pairs2.Count; i++)
                pairs2Count += pairs2[i].Count;

            for (int i = 0; i < pairs1.Count; i++)
            {
                for (int j = 0; j < pairs2.Count; j++)
                {
                    float similarity = (float)CalculatePairSimilarity(pairs1[i], pairs2[j]);
                    if (similarity >= minAcceptableSimilarity)
                    {
                        int pCount = pairs1[i].Count * pairs2[j].Count;
                        similarityCount += pCount;
                        sum += similarity * pCount * (pairs1[i].LengthAvg / lengthAvgOfPairs1 + pairs2[j].LengthAvg / lengthAvgOfPairs2) / 2;
                    }
                }
            }
            double res = (similarityCount / Math.Sqrt(pairs1Count * pairs2Count)) * 0.8 + (sum / Math.Max(1, similarityCount)) * 0.2;
            return Math.Min(1, res);
        }


        private static double CalculatePairsLengthAverage(List<ArcPair> pairs)
        {
            double sum = 0;
            foreach (ArcPair p in pairs)
            {
                sum += p.LengthAvg;
            }
            return sum / pairs.Count;
        }


        public void Dispose()
        {
            if (imagesTableAdapter != null)
                imagesTableAdapter.Dispose();
            if (shapesTableAdapter != null)
                shapesTableAdapter.Dispose();
            if (arcsTableAdapter != null)
                arcsTableAdapter.Dispose();
            if (arcRelationsTableAdapter != null)
                arcRelationsTableAdapter.Dispose();
        }
    }
}
