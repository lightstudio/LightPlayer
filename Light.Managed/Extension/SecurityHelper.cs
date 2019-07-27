#if ENABLE_STAGING
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Light.Managed.Extension
{
    internal class SecurityHelper
    {
#pragma warning disable 1998
        public static async Task<bool> VerifyHash(IEnumerable<KeyValuePair<string, string>> manifestData)
#pragma warning restore 1998
        {
            // TODO: Fill it later
            return true;
        }

#pragma warning disable 1998
        public static async Task<bool> VerifyManifestSecurity(string manifestData, string sigData)
#pragma warning restore 1998
        {
            // TODO: Fill it later
            return true;
        }
    }
}
#endif
