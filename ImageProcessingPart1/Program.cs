using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV.Util;
using static System.Net.Mime.MediaTypeNames;
using Emgu.CV.Ocl;
using Emgu.CV.Cuda;
using System.IO;

namespace ImageProcessingPart1
{
    internal class Program
    {
        private static string? ImagePath = @"D:\Channel\Code Sell\Working\Input";
        private static string? OutputPath = @"D:\Channel\Code Sell\Working\Output\test.png";
        static void Main(string[] args)
        {
            PreProcessingImage.Instance.ProcessImage(ImagePath);
        }

        public class PreProcessingImage
        {
            private string? ImagePath = string.Empty;
            private static PreProcessingImage? instance = null;
            private Mat? OrignalImage;
            private Mat? result;
            private double BinaryThreshold = 150;
            private double MaxValue = 255;

            public static PreProcessingImage Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new PreProcessingImage();
                    }
                    return instance;
                }
            }

            public void ProcessImage(string imgPath)
            {
                DirectoryInfo? directoryInfo = new DirectoryInfo(imgPath);
                foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                {
                    ImagePath = fileInfo.FullName;
                    ProcessImage(ImagePath, new Rectangle());
                }
            }
            public void ProcessImage(string filename, Rectangle rectangle)
            {
                ImagePath = filename;
                result = GetImage(ImreadModes.Grayscale);
                result = ResizeImage(result, result, 3500, 3500);
                result = RemoveNonZeroAreafromImage(result);
                result = GrayscaleToBinaryThreshold(result, BinaryThreshold, MaxValue, false);
                OrignalImage = result;
                result = SetImageBorder(result);
                GetHorizontalVertical(result, result.Rows / 150);
                OrignalImage = result;
                result.Save(OutputPath);
            }
            private Mat GetImage(ImreadModes imreadModes = ImreadModes.Unchanged)
            {
                Mat? Imagesize = new Mat(ImagePath);
                if (Imagesize.NumberOfChannels == 4 || Imagesize.NumberOfChannels == 3)
                {
                    CvInvoke.CvtColor(Imagesize, Imagesize, ColorConversion.Bgr2Gray);
                }
                else
                {
                    CvInvoke.CvtColor(Imagesize, Imagesize, ColorConversion.Gray2Bgr);
                }
                return Imagesize;
            }
            private Rectangle GetNonWhiteBounds(Bitmap bmp)
            {
                int minX = bmp.Width, minY = bmp.Height, maxX = 0, maxY = 0;
                Color white = Color.FromArgb(255, 255, 255);

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        Color pixel = bmp.GetPixel(x, y);
                        if (pixel.ToArgb() != white.ToArgb()) // Change this for tolerance
                        {
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (minX > maxX || minY > maxY)
                {
                    return new Rectangle(0, 0, 1, 1);
                }
                return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            private Mat RemoveNonZeroAreafromImage(Mat ThresholdImage)
            {
                using (Bitmap original = ThresholdImage.ToBitmap())
                {
                    Rectangle cropRect = GetNonWhiteBounds(original);
                    using (Bitmap cropped = original.Clone(cropRect, original.PixelFormat))
                    {
                        return cropped.ToMat();
                    }
                }
            }
            private Mat SetImageBorder(Mat ThresholdImage)
            {
                Image<Bgr, byte>? image = ThresholdImage.ToImage<Bgr, byte>();
                VectorOfPoint? points = new VectorOfPoint();
                CvInvoke.FindNonZero(image[ThresholdImage.NumberOfChannels].Mat, points);
                var lBoundingRectangle = new Mat(image[ThresholdImage.NumberOfChannels].Mat, CvInvoke.BoundingRectangle(points));

                int startRow = ((image.Rows - lBoundingRectangle.Rows) / 2) - 0;
                Rectangle rectangle = new Rectangle(5, startRow, image.Cols + 0, lBoundingRectangle.Rows + 0);
                image.Draw(rectangle, new Bgr(Color.White), 20);
                return image.Mat;
            }
            private Mat ResizeImage(Mat SourceImage, Mat SourceDestination, int Width, int Height)
            {
                CvInvoke.Resize(SourceImage, SourceDestination, new Size(Width, Height), 2, 2, Inter.Cubic);
                return SourceDestination;
            }
            private Mat GrayscaleToBinaryThreshold(Mat ThresholdImage, double binaryThreshold = 255, double Maxvalue = 255, bool IsCounter = false)
            {
                Image<Gray, byte>? toImage = ThresholdImage.ToImage<Gray, byte>();
                if (!IsCounter)
                    CvInvoke.Threshold(toImage, toImage, binaryThreshold, Maxvalue, ThresholdType.BinaryInv);
                else
                    CvInvoke.AdaptiveThreshold(toImage, toImage, Maxvalue, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 255, 0);

                toImage.Erode(1);
                toImage.Dilate(1);
                toImage.PyrDown();
                toImage.PyrUp();
                toImage.Canny(2, 6);
                toImage = toImage.SmoothGaussian(1, 1, 5, 5);//1, 1, 5, 5
                toImage = toImage.SmoothBilateral(5, 5, 5);
                toImage = toImage.SmoothBlur(1, 1, false);
                return toImage.Mat;
            }
            private List<System.Drawing.Rectangle> Contours2BBox(VectorOfVectorOfPoint contours)
            {
                List<System.Drawing.Rectangle>? list = new List<System.Drawing.Rectangle>();
                for (int i = 0; i < contours.Size; i++)
                {
                    if (contours[i].Length >= 1000)
                        list.Add(CvInvoke.BoundingRectangle(contours[i]));
                }

                return list;
            }
            private RotateFlipType OrientationToFlipType(int orientation)
            {
                return orientation switch
                {
                    6 => RotateFlipType.Rotate90FlipNone,
                    8 => RotateFlipType.Rotate270FlipNone,
                    _ => RotateFlipType.RotateNoneFlipNone,
                };
            }
            private VectorOfVectorOfPoint FilterContours(VectorOfVectorOfPoint contours, double threshold = 100)
            {
                var cells = new List<Rectangle>();
                for (int i = 0; i < contours.Size; i++)
                {
                    var area = CvInvoke.ContourArea(contours[i]);
                    if (area > 2000 && area < 200000)
                    {
                        var rect = CvInvoke.BoundingRectangle(contours[i]);
                        var aspectRatio = (double)rect.Width / rect.Height;
                        if (aspectRatio > 0.5 && aspectRatio <= 5)
                        {
                            cells.Add(rect);
                        }
                    }
                }
                VectorOfVectorOfPoint? filteredContours = new VectorOfVectorOfPoint();
                for (int i = 0; i < contours.Size; i++)
                {
                    if (CvInvoke.ContourArea(contours[i]) >= threshold)
                    {
                        filteredContours.Push(contours[i]);
                    }
                }
                return filteredContours;
            }
            private void GetHorizontalVertical(Mat ThresholdImage, int scale = 800)
            {
                MCvScalar mCvScalar = new MCvScalar(260);
                Point point = new Point(-1, -1);

                #region Horizontal lines            
                //Horizontal lines
                Mat? horizontal = ThresholdImage.Clone();
                int horizontalRow = horizontal.Rows / scale;
                int SizeXY = Convert.ToInt32(1);
                Mat? horizontalStructure = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(horizontalRow, SizeXY), point);
                CvInvoke.Erode(ThresholdImage, horizontal, horizontalStructure, new Point(-1, -1), -2, BorderType.Default, mCvScalar);
                CvInvoke.Dilate(horizontal, horizontal, horizontalStructure, new Point(-1, -1), 2, BorderType.Default, mCvScalar);
                #endregion

                #region Vertical lines
                Mat? vertical = ThresholdImage.Clone();
                int verticalCol = vertical.Cols / scale;
                Mat? verticalStructure = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(SizeXY, verticalCol), point);
                CvInvoke.Erode(ThresholdImage, vertical, verticalStructure, new Point(-1, -1), -2, BorderType.Default, mCvScalar);
                CvInvoke.Dilate(vertical, vertical, verticalStructure, new Point(-1, -1), Convert.ToInt32(1.5), BorderType.Default, mCvScalar);
                #endregion

                #region Both Horizontal & Vertical lines
                Mat? horizontalverticalMask = new Mat();
                CvInvoke.AddWeighted(horizontal, 10, vertical, 10, 10, horizontalverticalMask);

                Mat? bitxor = new Mat();
                CvInvoke.BitwiseXor(ThresholdImage, horizontalverticalMask, bitxor);
                CvInvoke.BitwiseAnd(bitxor, horizontalverticalMask, bitxor);
                CvInvoke.BitwiseOr(bitxor, horizontalverticalMask, bitxor);
                CvInvoke.BitwiseNot(bitxor, horizontalverticalMask);
                #endregion


            }
            private Image<Gray, byte> SetBorder(Image<Gray, byte> mat)
            {
                Image<Gray, byte> image = mat;
                {
                    int startCol = 1;
                    int startRow = 1;
                    int endCol = image.Cols;
                    int endRow = image.Rows;
                    Rectangle roi = new Rectangle(startCol, startRow, endCol - startCol, endRow - startRow);
                    image.Draw(roi, new Gray(150), 30, LineType.EightConnected);
                }
                return image;
            }
        }
    }
}
