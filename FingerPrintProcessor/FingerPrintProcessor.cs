using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;


namespace SimilarImageSearch.Engine.FingerPrintProcessor
{
    
    public static class FingerPrintProcessor
    {

        [SqlProcedure]
        public static int Xor(SqlBinary a, SqlBinary b, int xLength1, int yLength1, int xLength2, int yLength2, int xShift, int yShift)
        {
            bool[,] da = DecodeFingerPrint(a, xLength1, yLength1);
            bool[,] db = DecodeFingerPrint(b, xLength2, yLength2);

            return Xor(da, db, xShift, yShift);
        }

        private static int Xor(bool[,] a, bool[,] b, int xShift, int yShift)
        {
            int xLength1 = a.GetLength(1);
            int yLength1 = a.GetLength(0);
            int xLength2 = b.GetLength(1);
            int yLength2 = b.GetLength(0);

            int startX1, startX2, startY1, startY2;
            int endX1, endX2, endY1, endY2;

            if (xShift >= 0)
            {
                startX1 = xShift;
                startX2 = 0;
                endX1 = xLength1;
                endX2 = xLength2 - xShift;
            }
            else
            {
                startX1 = 0;
                startX2 = -xShift;
                endX1 = xLength1 + xShift;
                endX2 = xLength2;
            }
            if (yShift >= 0)
            {
                startY1 = yShift;
                startY2 = 0;
                endY1 = yLength1;
                endY2 = yLength2 - yShift;
            }
            else
            {
                startY1 = 0;
                startY2 = -yShift;
                endY1 = yLength1 + yShift;
                endY2 = yLength2;
            }

            int res = 0;

            for (int i1 = startY1, i2 = startY2; i1 < endY1 && i2 < endY2; i1++, i2++)
            {
                for (int j1 = startX1, j2 = startX2; j1 < endX1 && j2 < endX2; j1++, j2++)
                {
                    if (a[i1, j1] ^ b[i2, j2])
                        res++;
                }
            }

            return res;
        }

        [SqlProcedure]
        public static int OneCount(SqlBinary a)
        {
            return OneCount(a.Value);
        }

        public static int OneCount(byte[] a)
        {
            int res = 0;
            for (int i = 0; i < a.Length; i++)
            {
                res += OneCount(a[i]);
            }
            return res;
        }

        private static byte OneCount(byte b)
        {
            byte o = 0;
            for (byte i = 0; i < 8; i++)
            {
                o += (byte)(b % 2 == 1 ? 1 : 0);
                b >>= 1;
            }
            return o;
        }


        [SqlProcedure]
        public static int CompareFingerPrints(SqlBinary a, SqlBinary b,int xLength, int yLength, int deviance, int resize, int acceptable)
        {
            return CompareFingerPrints(DecodeFingerPrint(a, xLength, yLength), DecodeFingerPrint(b, xLength, yLength), deviance, resize, acceptable);
        }

        public static int CompareFingerPrints(bool[,] a, bool[,] b, int deviance, int resize, int acceptable)
        {
            if (a.Length != b.Length)
                throw new Exception("fingerprints are not equal");
            
            int xLength = a.GetLength(1);
            int yLength = a.GetLength(0);

            int xorRes, x1Length, x2Length, y1Length, y2Length;
            int minXorRes = int.MaxValue;
            bool[,] originalA = a;
            bool[,] originalB = b;

            for (int ri = 0; ri <= resize; ri++)
            {
                for (int rj = 0; rj <= resize; rj++)
                {
                    for (int rt = 0; rt <= 1; rt++)
                    {
                        if (resize > 0)
                        {
                            if (rt == 0)
                            {
                                x1Length = xLength - rj;
                                y1Length = yLength - ri;
                                x2Length = xLength;
                                y2Length = yLength;
                                a = ResizeFingerPrint(originalA, x1Length, y1Length);
                                b = originalB;
                            }
                            else
                            {
                                x1Length = xLength;
                                y1Length = yLength;
                                x2Length = xLength - rj;
                                y2Length = yLength - ri;
                                a = originalA;
                                b = ResizeFingerPrint(originalB, x2Length, y2Length);
                            }
                        }

                        xorRes = Xor(a, b, 0, 0);
                        if (xorRes < acceptable)
                            return xorRes;
                        else if (xorRes < minXorRes)
                            minXorRes = xorRes;

                        for (int di = 1; di < deviance; di++)
                        {
                            for (int dj = -di; dj < di; dj++)
                            {
                                xorRes = Xor(a, b, dj, -di);
                                if (xorRes < acceptable)
                                    return xorRes;
                                else if (xorRes < minXorRes)
                                    minXorRes = xorRes;

                                xorRes = Xor(a, b, di, dj);
                                if (xorRes < acceptable)
                                    return xorRes;
                                else if (xorRes < minXorRes)
                                    minXorRes = xorRes;

                                xorRes = Xor(a, b, -dj, di);
                                if (xorRes < acceptable)
                                    return xorRes;
                                else if (xorRes < minXorRes)
                                    minXorRes = xorRes;

                                xorRes = Xor(a, b, -di, -dj);
                                if (xorRes < acceptable)
                                    return xorRes;
                                else if (xorRes < minXorRes)
                                    minXorRes = xorRes;
                            }
                        }
                    }
                }
            }
            return minXorRes;
        }

        private static bool[,] ResizeFingerPrint(bool[,] bin, int newXLength, int newYLength)
        {
            if (bin.GetLength(0) == newYLength && bin.GetLength(1) == newXLength)
                return bin;
            bool[,] resizedData = new bool[newYLength, newXLength];
            double oldXLength = bin.GetLength(1);
            double oldYLength = bin.GetLength(0);
            for (int i = 0; i < newYLength; i++)
            {
                for (int j = 0; j < newXLength; j++)
                {
                    int ii = (int)Math.Round(i * oldYLength / newYLength);
                    int jj = (int)Math.Round(j * oldXLength / newXLength);
                    resizedData[i, j] = bin[ii, jj];
                }
            }
            return resizedData;
        }

        public static bool[,] DecodeFingerPrint(SqlBinary bin, int xLength, int yLength)
        {
            return DecodeFingerPrint(bin.Value, xLength, yLength);
        }

        public static bool[,] DecodeFingerPrint(byte[] bin, int xLength, int yLength)
        {
            bool[,] res = new bool[yLength, xLength];
            for (int i = 0; i < yLength; i++)
            {
                for (int j = 0; j < xLength; j++)
                {
                    res[i, (j/8)*8+(7 -(j%8))] = ((bin[(i * yLength + j) / 8] >> (j % 8)) % 2) == 1;
                }
            }
            return res;
        }
    }

}
