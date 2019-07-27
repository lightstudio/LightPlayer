using System;

namespace Light.Managed.Tools
{
    internal static class ExternalFileHelper
    {
        public static string GetFileName(string fileId)
        {
            if(fileId == null) throw new ArgumentNullException();
            // External.xx
            return fileId.Substring(9);
        }
    }
}
