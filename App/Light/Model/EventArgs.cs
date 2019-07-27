using Light.Managed.Database.Entities;

namespace Light.Model
{
    internal class MediaItemChangedEventArgs
    {
        public bool IsFirst { get; }
        public bool IsLast { get; }
        public string FileId { get; }
        public DbMediaFile File { get; set; }
        public uint Seq { get; set; }

        public MediaItemChangedEventArgs(string fileId, bool isFirst, bool isLast)
        {
            FileId = fileId;
            IsFirst = isFirst;
            IsLast = isLast;
        }

        public MediaItemChangedEventArgs(DbMediaFile file, bool isFirst, bool isLast, uint seqId)
        {
            File = file;
            IsFirst = isFirst;
            IsLast = isLast;
            // For compatibility
            if (File?.Id != null) FileId = File?.Id.ToString();
            else FileId = "-65535";
            Seq = seqId;
        }
    }
}
