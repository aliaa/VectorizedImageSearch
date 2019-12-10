using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using SimilarImageSearch.Engine;
using System.IO;

namespace SimilarImageSearch.Test
{
    [TestClass]
    public class Misc
    {
        [TestMethod]
        public void TestMD5()
        {
            MD5 md5Hasher = new MD5CryptoServiceProvider();
            Dictionary<string, byte[]> hashes = new Dictionary<string,byte[]>();
            foreach (string file in ReverseImageSearchEngine.SearchImageFiles(@"C:\Users\solaris\Desktop\pic test\test shapes"))
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    hashes.Add(file, md5Hasher.ComputeHash(fs)); ;
                }
            }

            //md5Hasher = new MD5CryptoServiceProvider();
            string imagefile = @"C:\Users\solaris\Desktop\pic test\test shapes\testShape2.png";
            byte[] hash;
            using (FileStream fs = new FileStream(imagefile, FileMode.Open, FileAccess.Read))
            {
                hash = md5Hasher.ComputeHash(fs);
            }

            System.Diagnostics.Debug.WriteLine(CompareBytes(hash, hashes[imagefile]));
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
    }

}
