using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ImageDatasetTrainTestSplit
{
    class ImageSquarer : IDisposable
    {
        private int _biggestDimension;
        private bool _heightIsBiggestDimension;
        private Bitmap _originalImage;
        private Bitmap _squareImage;

        public ImageSquarer(string fileName)
        {
            _originalImage = (Bitmap)Image.FromFile(fileName);
            if (_originalImage.Height > _originalImage.Width)
            {
                _biggestDimension = _originalImage.Height;
                _heightIsBiggestDimension = true;
            }
            else
            {
                _biggestDimension = _originalImage.Width;
                _heightIsBiggestDimension = false;
            }
        }

        public void GetSquareImage()
        {
            if (_originalImage.Width == _originalImage.Height)
            {
                _squareImage = _originalImage;
                return;
            }

            var orignalBitmap = _originalImage;

            if (_originalImage.PixelFormat == PixelFormat.Indexed
                || _originalImage.PixelFormat == PixelFormat.Format1bppIndexed
                || _originalImage.PixelFormat == PixelFormat.Format4bppIndexed
                || _originalImage.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                var tempBmp = new Bitmap(_originalImage.Width, _originalImage.Height);
                var tempGraphics = Graphics.FromImage(tempBmp);
                tempGraphics.DrawImage(_originalImage, 0, 0);
                orignalBitmap = tempBmp;
            }

            var newImage = new Bitmap(_biggestDimension, _biggestDimension, Graphics.FromImage(orignalBitmap));
            var graphics = Graphics.FromImage(newImage);
            graphics.FillRectangle(new SolidBrush(Color.Black), new Rectangle(0, 0, newImage.Width, newImage.Height));

            if (_heightIsBiggestDimension)
            {
                int difference = _originalImage.Height - _originalImage.Width;
                int padding = (int)Math.Floor((double)difference / 2.0);
                graphics.DrawImage(_originalImage, new Point(padding, 0));
            }
            else
            {
                int difference = _originalImage.Width - _originalImage.Height;
                int padding = (int)Math.Floor((double)difference / 2.0);
                graphics.DrawImage(_originalImage, new Point(0, padding));
            }

            _squareImage = newImage;
        }

        public bool SaveToFile(string fileFullName)
        {
            if (_squareImage == null)
                return false;

            if (File.Exists(fileFullName))
                File.Delete(fileFullName);

            _squareImage.Save(fileFullName);

            return true;
        }

        public void Dispose()
        {
            _originalImage?.Dispose();
            _squareImage?.Dispose();
        }
    }
}
