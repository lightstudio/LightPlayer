using Light.Common;
using Light.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;

namespace Light.Phone.View
{
    public static class SharedUtils
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            var buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
        public static async void ConfirmRefreshLibrary()
        {
            var dialog = new MessageDialog(CommonSharedStrings.MediaLibraryScanWarningText, CommonSharedStrings.Warning);
            dialog.Commands.Add(new UICommand(
                CommonSharedStrings.ConfirmString,
                async (c) =>
                {
                    await LibraryService.IndexAsync(new ThumbnailOperations());
                }));
            dialog.Commands.Add(new UICommand(CommonSharedStrings.CancelString));
            await dialog.ShowAsync();
        }
    }
}
