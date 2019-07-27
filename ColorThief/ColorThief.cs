using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ColorThiefDotNet
{
    public class ColorThief
    {
        public const int DefaultColorCount = 5;
        public const int DefaultQuality = 10;
        public const bool DefaultIgnoreWhite = true;
        public const int ColorDepth = 4;

        public QuantizedColor GetColor(byte[] source, int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite)
        {
            var palette = GetPalette(source, 2, quality, ignoreWhite);
            var dominantColor = palette.FirstOrDefault();
            return dominantColor;
        }

        public List<QuantizedColor> GetPalette(byte[] source, int colorCount = DefaultColorCount, int quality = DefaultQuality, bool ignoreWhite = DefaultIgnoreWhite)
        {
            var p = ConvertPixels(source, source.Length / 4, quality, ignoreWhite);
            var cmap = GetColorMap(p, colorCount);
            return cmap != null ? cmap.GeneratePalette() : new List<QuantizedColor>();
        }

        private async Task<byte[]> GetIntFromPixel(BitmapDecoder decoder)
        {
            var pixelsData = await decoder.GetPixelDataAsync();
            var pixels = pixelsData.DetachPixelData();
            return pixels;
        }

        private async Task<byte[][]> GetPixelsFast(BitmapDecoder sourceImage, int quality, bool ignoreWhite)
        {
            if (quality < 1)
            {
                quality = DefaultQuality;
            }

            var pixels = await GetIntFromPixel(sourceImage);
            var pixelCount = sourceImage.PixelWidth * sourceImage.PixelHeight;

            return ConvertPixels(pixels, Convert.ToInt32(pixelCount), quality, ignoreWhite);
        }

        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors.
        /// </summary>
        /// <param name="pixelArray">Pixel array.</param>
        /// <param name="colorCount">The color count.</param>
        /// <returns></returns>
        private CMap GetColorMap(byte[][] pixelArray, int colorCount)
        {
            // Send array to quantize function which clusters values using median
            // cut algorithm
            var cmap = Mmcq.Quantize(pixelArray, colorCount);
            return cmap;
        }

        private byte[][] ConvertPixels(byte[] pixels, int pixelCount, int quality, bool ignoreWhite)
        {


            var expectedDataLength = pixelCount * ColorDepth;
            if (expectedDataLength != pixels.Length)
            {
                throw new ArgumentException("(expectedDataLength = "
                                            + expectedDataLength + ") != (pixels.length = "
                                            + pixels.Length + ")");
            }

            // Store the RGB values in an array format suitable for quantize
            // function

            // numRegardedPixels must be rounded up to avoid an
            // ArrayIndexOutOfBoundsException if all pixels are good.

            var numRegardedPixels = (pixelCount + quality - 1) / quality;

            var numUsedPixels = 0;
            var pixelArray = new byte[numRegardedPixels][];

            for (var i = 0; i < pixelCount; i += quality)
            {
                var offset = i * ColorDepth;
                var b = pixels[offset];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                var a = pixels[offset + 3];

                // If pixel is mostly opaque and not white
                if (a >= 125 && !(ignoreWhite && r > 250 && g > 250 && b > 250))
                {
                    pixelArray[numUsedPixels] = new[] { r, g, b };
                    numUsedPixels++;
                }
            }

            // Remove unused pixels from the array
            var copy = new byte[numUsedPixels][];
            Array.Copy(pixelArray, copy, numUsedPixels);
            return copy;
        }
    }
}