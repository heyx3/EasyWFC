using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace QM2D
{
    public static class Utilities
    {
        public static BitmapSource Convert(Color[,] inData)
        {
            int stride = inData.GetLength(0) * 4,
                size = inData.GetLength(1) * stride;
            byte[] pixels = new byte[size];
            
            for (int y = 0; y < inData.GetLength(1); ++y)
                for (int x = 0; x < inData.GetLength(0); ++x)
                {
                    int index = (y * stride) + (4 * x);
                    //Using 32-bit BGRA pixels.
                    pixels[index] = inData[x, y].B;
                    pixels[index + 1] = inData[x, y].G;
                    pixels[index + 2] = inData[x, y].R;
                    pixels[index + 3] = inData[x, y].A;
                }
            
            return BitmapSource.Create(inData.GetLength(0), inData.GetLength(1),
                                       96.0, 96.0, PixelFormats.Bgra32, null,
                                       pixels, stride);
        }
        public static void Convert(BitmapSource inImage, ref Color[,] outData)
        {
            if (inImage.Format != PixelFormats.Bgra32)
                inImage = new FormatConvertedBitmap(inImage, PixelFormats.Bgra32, null, 0);

            //Get pixel byte data.
            int stride = inImage.PixelWidth * 4;
            int size = inImage.PixelHeight * stride;
            byte[] pixels = new byte[size];
            inImage.CopyPixels(pixels, stride, 0);

            //Make sure the pixel grid is the right size.
            if (outData.GetLength(0) != inImage.PixelWidth ||
                outData.GetLength(1) != inImage.PixelHeight)
            {
                outData = new Color[inImage.PixelWidth, inImage.PixelHeight];
            }

            //Set the pixel grid.
            for (int y = 0; y < inImage.PixelHeight; ++y)
                for (int x = 0; x < inImage.PixelWidth; ++x)
                {
                    int index = (y * stride) + (4 * x);
                    outData[x, y] = Color.FromArgb(pixels[index + 3],
                                                   pixels[index + 2],
                                                   pixels[index + 1],
                                                   pixels[index]);
                }
        }

        /// <summary>
        /// Returns an error message if something went wrong, or "null" if everything went OK.
        /// </summary>
        public static string ToFile(BitmapSource img, string filePath)
        {
            string fileType = Path.GetExtension(filePath).Replace(".", "");

            BitmapEncoder encoder = null;
            switch (fileType)
            {
                case "png": encoder = new PngBitmapEncoder(); break;
                case "bmp": encoder = new BmpBitmapEncoder(); break;
                case "jpg":
                case "jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;

                default:
                    return "Unknown extension: " + fileType;
            }
            encoder.Frames.Add(BitmapFrame.Create(img));

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
            }
            catch (Exception e)
            {
                return "Error saving file to " + filePath+ ":\n" +
                       "(" + e.GetType().Name + "): " + e.Message;
            }

            return null;
        }
        /// <summary>
        /// Returns an error message if someting went wrong, or "null" if everything went OK.
        /// </summary>
        public static string FromFile(string filePath, out BitmapImage img)
        {
            //Read the file data.
            byte[] bmpData = null;
            try
            {
                bmpData = File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                img = null;
                return "Error reading image file:\n(" + e.GetType().Name + "): " + e.Message;
            }

            //Deserialize the file data.
            try
            {
                using (var stream = new MemoryStream(bmpData))
                {
                    img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = stream;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                }
            }
            catch (Exception e)
            {
                img = null;
                return "Error deserializing image file:\n(" + e.GetType().Name + "): " + e.Message;
            }

            return null;
        }
    }
}
