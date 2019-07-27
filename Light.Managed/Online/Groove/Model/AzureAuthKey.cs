using Newtonsoft.Json;

namespace Light.Managed.Online.Groove.Model
{
    /// <summary>
    /// Represents Azure ACS auth token.
    /// </summary>
    public class AzureAuthKey
    {
        /// <summary>
        /// Access token.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Token type.
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Token's validity in seconds.
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresInSecs { get; set; }

        /// <summary>
        /// Token scope.
        /// </summary>
        [JsonProperty("scope")]
        public string Scope { get; set; }
    }
}
