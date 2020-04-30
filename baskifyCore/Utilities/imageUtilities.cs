using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace baskifyCore.Utilities
{
    public static class imageUtilities
    {
        public static string uploadFile(IFormFile file, string webroot, string path, int height, int width, string filename)
        {
            try
            {
                Bitmap bitmap;
                using (var inputStream = file.OpenReadStream())
                {
                    bitmap = new Bitmap(inputStream);


                    var origHeight = bitmap.Height;
                    var origWidth = bitmap.Width;

                    var targetRatio = (double)height / width; //1:3 for example (.333)
                    if ((double)origHeight / origWidth > targetRatio)
                    { //height needs to be cropped down
                        var containerHeight = (int)Math.Floor(origWidth * targetRatio);
                        bitmap = CropImage(bitmap, 0, (origHeight - containerHeight) / 2, origWidth, containerHeight);
                    }
                    else if ((double)origHeight / origWidth < targetRatio)
                    { //width needs to be cropped down
                        var containerWidth = (int)Math.Floor(origHeight * (1 / targetRatio));
                        bitmap = CropImage(bitmap, (origWidth-containerWidth) / 2, 0, containerWidth, bitmap.Height);
                    }

                    //NOW RESIZE NOW THAT RATIO MATCHES
                    var newBitmap = Resize(bitmap, width, height);


                    //var extension = Path.GetExtension(file.FileName);
                    var physicalFilename = webroot + "/" + path.Trim('/') + "/" + filename + ".jpeg"; //should be safe characters
                    using (var fs = new FileStream(Path.GetFullPath(physicalFilename), FileMode.Create))
                    {
                        newBitmap.Save(fs, ImageFormat.Jpeg);
                        newBitmap.Dispose();
                    }
                    return "/" + path.Trim('/') + "/" + filename + ".jpeg";
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static Bitmap CropImage(Bitmap sourceBitmap, int x, int y, int width, int height)
        {
        //Parsing stream to bitmap
            //Get new dimensions
            double sourceWidth = Convert.ToDouble(sourceBitmap.Size.Width);
            double sourceHeight = Convert.ToDouble(sourceBitmap.Size.Height);
            Rectangle cropRect = new Rectangle(x, y, width, height);

            //Creating new bitmap with valid dimensions, DONT DISPOSE
            Bitmap newBitMap = new Bitmap(cropRect.Width, cropRect.Height);
            
                using (Graphics g = Graphics.FromImage(newBitMap))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    g.DrawImage(sourceBitmap, new Rectangle(0, 0, newBitMap.Width, newBitMap.Height), cropRect, GraphicsUnit.Pixel);  
                }
            return newBitMap;

        }

        public static Bitmap Resize(Image current, int width, int height)
        {
            var canvas = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(canvas))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(current, 0, 0, width, height);
            }

            return canvas;
        }

        /// <summary>
        /// Makes a photo square given a length, or a rectangle given a length and a width...
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Bitmap ResizePhoto(Bitmap bmp, int width, int length)
        {
            Bitmap res = new Bitmap(width, length);
            Graphics g = Graphics.FromImage(res);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, width, length);
            int t = 0, l = 0;
            if (bmp.Height > bmp.Width)
                t = (bmp.Height - bmp.Width) / 2;
            else
                l = (bmp.Width - bmp.Height) / 2;
            g.DrawImage(bmp, new Rectangle(0, 0, width, length), new Rectangle(l, t, bmp.Width - l * 2, bmp.Height - t * 2), GraphicsUnit.Pixel);
            return res;
        }
    }
}
