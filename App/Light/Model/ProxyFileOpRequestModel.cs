using System.Collections.Generic;
using Windows.Media.Playback;
using Light.Managed.Database.Entities;

namespace Light.Model
{
    public class ProxyFileOpRequestModel
    {
        public ProxyFileOpControlType Type { get; set; }
        public List<DbMediaFile> FilesToAdd { get; set; }
        public List<MediaPlaybackItem> ItemsToRemove { get; set; }
        public ProxyAddFileRequestParams AddAdditionalParams { get; set; }

        public ProxyFileOpRequestModel(List<MediaPlaybackItem> itemsToRemove)
        {
            Type = ProxyFileOpControlType.Delete;
            ItemsToRemove = itemsToRemove;

            FilesToAdd = new List<DbMediaFile>();
            AddAdditionalParams = new ProxyAddFileRequestParams();
        }

        public ProxyFileOpRequestModel(List<DbMediaFile> filesToAdd, bool requireClear = false, bool isInsertion = false)
        {
            Type = ProxyFileOpControlType.Add;
            FilesToAdd = filesToAdd;

            ItemsToRemove = new List<MediaPlaybackItem>();
            AddAdditionalParams = new ProxyAddFileRequestParams(requireClear, isInsertion);
        }
    }

    public class ProxyAddFileRequestParams
    {
        public bool RequireClear { get; set; }
        public bool IsInsertion { get; set; }

        public ProxyAddFileRequestParams()
        {
            RequireClear = IsInsertion = false;
        }

        public ProxyAddFileRequestParams(bool requireClear, bool isInsertion)
        {
            RequireClear = requireClear;
            IsInsertion = isInsertion;
        }
    }
}