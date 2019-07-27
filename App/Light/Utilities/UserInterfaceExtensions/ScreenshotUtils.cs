using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Light.Utilities.UserInterfaceExtensions
{
    public static class ScreenshotUtils
    {
        public static async Task<string> GetImageAsync(this UIElement element)
        {
            // Get a current screenshot
            var renderTargetmap = new RenderTargetBitmap();
            await renderTargetmap.RenderAsync(element);

            // Get current display info
            var displayInfo = DisplayInformation.GetForCurrentView();

            if (displayInfo == null) return string.Empty;

            // Save image
            var imageFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{Guid.NewGuid()}.png");
            using (var imageFileStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Get pixel array
                var imagePixelsBuffer = await renderTargetmap.GetPixelsAsync();

                // Set up encoder
                var imageEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, imageFileStream);

                // Write pixels
                imageEncoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)renderTargetmap.PixelWidth,
                    (uint)renderTargetmap.PixelHeight,
                    displayInfo.RawDpiX,
                    displayInfo.RawDpiY,
                    imagePixelsBuffer.ToArray());

                // Flush stream
                await imageEncoder.FlushAsync();
            }

            return imageFile.Path;
        }
    }
}
