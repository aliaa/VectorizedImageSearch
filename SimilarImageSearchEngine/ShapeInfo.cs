using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace SimilarImageSearch.Engine
{
    public class ShapeInfo
    {
        public ShapeInfo(int imageID, int shapeID, string imagePath, Size imageSize, Size ShapeSize, int fileLength, float similarity)
            : this(imageID, shapeID, imagePath, imageSize, ShapeSize, fileLength, similarity, string.Empty)
        { }

        public ShapeInfo(int imageID,int shapeID, string imagePath, Size imageSize, Size ShapeSize, int fileLength, float similarity, string tags)
        {
            this.ImageID = imageID;
            this.ShapeID = shapeID;
            this.FileLength = fileLength;
            this.ImagePath = imagePath;
            this.ImageSize = imageSize;
            this.ShapeSize = ShapeSize;
            this.Similarity = similarity;
            this.Tags = tags;
        }

        public ShapeInfo() { }

        public int ImageID { get; set; }
        public int ShapeID { get; set; }

        public string ImagePath { get; set; }

        public Size ImageSize { get; set; }

        public Size ShapeSize { get; set; }

        public int FileLength { get; set; }

        public float Similarity { get; set; }

        public string Tags { get; set; }
    }
}
