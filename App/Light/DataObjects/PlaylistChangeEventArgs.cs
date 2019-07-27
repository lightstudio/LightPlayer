using System.Collections.Generic;
using Light.Managed.Database.Entities;

namespace Light.DataObjects
{
    internal class PlaylistChangeEventArgs
    {
        public IReadOnlyList<DbMediaFile> AffectedFiles { get; set; }
        public int StartIndex { get; set; }
        public ChangeOperationType Type { get; set; }

        public PlaylistChangeEventArgs(IReadOnlyList<DbMediaFile> affecteDbMediaFiles, int startIndex,
            ChangeOperationType type)
        {
            AffectedFiles = affecteDbMediaFiles;
            StartIndex = startIndex;
            Type = type;
        }
    }

    internal enum ChangeOperationType
    {
        Add = 0,
        Modify = 1,
        Delete = 2
    }
}
