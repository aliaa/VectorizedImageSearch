using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageConversion
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter source folder path: ");
            string sourcePath = Console.ReadLine();
            Console.WriteLine("Enter destination folder path: ");
            string destPath = Console.ReadLine();
            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);
            Console.WriteLine("Enter output format (jpg, png, gif, bmp): ");
            string format = Console.ReadLine();
            if(format != "jpg" && format != "png" && format != "gif")
            {
                Console.WriteLine("invalid format!");
                return;
            }
            ImageFormat imageFormat;
            switch (format)
            {
                case "png":
                    imageFormat = ImageFormat.Png;
                    break;
                case "gif":
                    imageFormat = ImageFormat.Gif;
                    break;
                case "bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;
                default:
                    imageFormat = ImageFormat.Jpeg;
                    break;
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                Image image = null;
                try
                {
                    image = Image.FromFile(file);
                }
                catch
                {
                    continue;
                }
                string newName = Path.GetFileNameWithoutExtension(file) + "." + format;
                string newPath = Path.Combine(destPath, newName);
                image.Save(newPath, imageFormat);
            }
            Console.WriteLine("Finished!");
        }
    }
}
