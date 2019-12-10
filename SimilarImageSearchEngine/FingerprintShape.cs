using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using AForge.Imaging;

namespace SimilarImageSearch.Engine
{
    public class FingerprintShape
    {
        public static readonly Size SmallFingerprintSize = new Size(48, 48);
        public static readonly Size MediumFingerprintSize = new Size(128, 128);
        public static readonly Size LargeFingerprintSize = new Size(200, 200);
        
        public static readonly Size MinimumAcceptableShapeSize = LargeFingerprintSize;

        public byte[] SmallFingerPrint { get; set; }
        public byte[] MediumFingerPrint { get; set; }
        public byte[] LargeFingerPrint { get; set; }


        public FingerprintShape()
        { }

        public FingerprintShape(Bitmap image)
        {
            this.uimg = UnmanagedImage.FromManagedImage(image);
        }

        public FingerprintShape(UnmanagedImage image)
        {
            this.uimg = image;
        }


        public void MakeFingerprints()
        {
            if (uimg == null)
                throw new NullReferenceException("uimg is null.");
            BaseResizeFilter resizeAlgorithm = new ResizeNearestNeighbor(LargeFingerprintSize.Width, LargeFingerprintSize.Height);
            UnmanagedImage FPUimg = resizeAlgorithm.Apply(uimg);
            byte[] fp = new byte[LargeFingerprintSize.Height * LargeFingerprintSize.Width / 8];
            unsafe
            {
                byte* p = (byte*)FPUimg.ImageData.ToPointer();
                for (int i = 0; i < fp.Length; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        fp[i] += (byte)((*p > 127) ? 1 : 0);
                        fp[i] <<= 1;
                        p++;
                    }
                }
            }
            this.LargeFingerPrint = fp;
            FPUimg.Dispose();

            resizeAlgorithm = new ResizeNearestNeighbor(MediumFingerprintSize.Width, MediumFingerprintSize.Height);
            FPUimg = resizeAlgorithm.Apply(uimg);
            fp = new byte[MediumFingerprintSize.Height * MediumFingerprintSize.Width / 8];
            unsafe
            {
                byte* p = (byte*)FPUimg.ImageData.ToPointer();
                for (int i = 0; i < fp.Length; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        fp[i] += (byte)((*p > 127) ? 1 : 0);
                        fp[i] <<= 1;
                        p++;
                    }
                }
            }
            this.MediumFingerPrint = fp;
            FPUimg.Dispose();

            resizeAlgorithm = new ResizeNearestNeighbor(SmallFingerprintSize.Width, SmallFingerprintSize.Height);
            FPUimg = resizeAlgorithm.Apply(uimg);
            fp = new byte[SmallFingerprintSize.Height * SmallFingerprintSize.Width / 8];
            unsafe
            {
                byte* p = (byte*)FPUimg.ImageData.ToPointer();
                for (int i = 0; i < fp.Length; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        fp[i] += (byte)((*p > 127) ? 1 : 0);
                        fp[i] <<= 1;
                        p++;
                    }
                }
            }
            this.SmallFingerPrint = fp;
            FPUimg.Dispose();
        }


        public Bitmap SmallFingerPrintImage
        {
            get
            {
                return MakeImageFromFingerprint(FingerPrintSize.Small);
            }
        }


        public Bitmap MediumFingerPrintImage
        {
            get
            {
                return MakeImageFromFingerprint(FingerPrintSize.Medium);
            }
        }


        public Bitmap LargeFingerPrintImage
        {
            get
            {
                return MakeImageFromFingerprint(FingerPrintSize.Large);
            }
        }


        private Bitmap MakeImageFromFingerprint(FingerPrintSize FPSize)
        {
            Size size;
            byte[] data = null;
            switch (FPSize)
            {
                case FingerPrintSize.Small:
                    size = SmallFingerprintSize;
                    data = SmallFingerPrint;
                    break;
                case FingerPrintSize.Medium:
                    size = MediumFingerprintSize;
                    data = MediumFingerPrint;
                    break;
                case FingerPrintSize.Large:
                    size = LargeFingerprintSize;
                    data = LargeFingerPrint;
                    break;
                default:
                    throw new Exception();
            }
            if (data == null || data.Length != size.Width * size.Height / 8)
                return null;

            Bitmap res = new Bitmap(size.Width, size.Height);
            BitmapData bmpData = res.LockBits(new Rectangle(new Point(0, 0), size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            byte[] binData = new byte[size.Width * size.Height];
            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    bool d = (data[i] >> (j % 8)) % 2 == 1;
                    binData[i * 8 + 7 - j] = (byte)(d ? 255 : 0);
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(binData, 0, bmpData.Scan0, binData.Length);
            res.UnlockBits(bmpData);
            return res;
        }



        public UnmanagedImage uimg { get; set; }
    }



    enum FingerPrintSize
    {
        Small, Medium, Large
    }
}
