using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.StartScreen;
using Light.Common;

namespace Light.Shell
{
    class JumplistHelper
    {
        /// <summary>
        /// This property is used to determine the presence of Jumplist class.
        /// </summary>
        public static bool IsJumplistPresent => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 2, 0);

        public async Task ProvisionJumplistAsync()
        {
            if (!IsJumplistPresent) throw new NotSupportedException("Jumplist requires contract Windows.Foundation.UniversalApiContract, version 2.0.");

            try
            {
                var jumpList = await JumpList.LoadCurrentAsync();
                jumpList.Items.Clear();

                var albumMenuEntry = JumpListItem.CreateWithArguments("light-jumplist:viewallalbums", 
                    CommonSharedStrings.JumplistAlbumText);

                albumMenuEntry.GroupName = CommonSharedStrings.CategoryGroupName;
                albumMenuEntry.Description = CommonSharedStrings.JumplistAlbumText;

                albumMenuEntry.Logo = new Uri(CommonSharedStrings.JumplistAlbumIconPath);

                var artistMenuEntry = JumpListItem.CreateWithArguments("light-jumplist:viewallartists",
                    CommonSharedStrings.JumplistArtistText);

                artistMenuEntry.GroupName = CommonSharedStrings.CategoryGroupName;
                artistMenuEntry.Description = CommonSharedStrings.JumplistArtistText;

                artistMenuEntry.Logo = new Uri(CommonSharedStrings.JumplistArtistIconPath);

                var songMenuEntry = JumpListItem.CreateWithArguments("light-jumplist:viewallsongs",
                    CommonSharedStrings.JumplistSongText);

                songMenuEntry.GroupName = CommonSharedStrings.CategoryGroupName;
                songMenuEntry.Description = CommonSharedStrings.JumplistSongText;

                songMenuEntry.Logo = new Uri(CommonSharedStrings.JumplistSongIconPath);

                var playlistMenuEntry = JumpListItem.CreateWithArguments("light-jumplist:viewallplaylist",
                    CommonSharedStrings.JumplistPlaylistText);

                playlistMenuEntry.GroupName = CommonSharedStrings.CategoryGroupName;
                playlistMenuEntry.Description = CommonSharedStrings.JumplistPlaylistText;

                playlistMenuEntry.Logo = new Uri(CommonSharedStrings.JumplistPlaylistIconPath);

                jumpList.Items.Add(albumMenuEntry);
                jumpList.Items.Add(artistMenuEntry);
                jumpList.Items.Add(songMenuEntry);
                jumpList.Items.Add(playlistMenuEntry);

                await jumpList.SaveAsync();
            }
            catch
            {
                // Ignore
            }
        }
    }
}
