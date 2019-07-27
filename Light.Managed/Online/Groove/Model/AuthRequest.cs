using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Light.Managed.Online.Groove.Model
{
    /// <summary>
    /// Represents Azure ACS authentication request parameters.
    /// </summary>
    public class AuthRequest
    {
        /// <summary>
        /// Client ID.
        /// </summary>
        [Display(Name = "client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret.
        /// </summary>
        [Display(Name = "client_secret")]
        public string ClientKey { get; set; }

        /// <summary>
        /// Requested scope.
        /// </summary>
        [Display(Name = "scope")]
        public string Scope { get; set; }

        /// <summary>
        /// Grant types.
        /// </summary>
        [Display(Name = "grant_type")]
        public string GrantType { get; set; }

        /// <summary>
        /// Initializes new instance of <see cref="AuthRequest"/>.
        /// </summary>
        public AuthRequest()
        {
            ClientId = ClientKey = Scope = GrantType = string.Empty;
        }

        /// <summary>
        /// Initializes new instance of <see cref="AuthRequest"/>.
        /// </summary>
        /// <param name="clientId">Client ID.</param>
        /// <param name="clientKey">Client Key.</param>
        /// <param name="scope">Requested scope.</param>
        /// <param name="grantType">Grant type.</param>
        public AuthRequest(string clientId, string clientKey, string scope, string grantType)
        {
            ClientId = clientId;
            ClientKey = clientKey;
            Scope = scope;
            GrantType = grantType;
        }

        /// <summary>
        /// Convert request to key-value dictionary for further usage.
        /// </summary>
        /// <returns>Instance of <see cref="Dictionary{string, string}"/></returns>
        public Dictionary<string, string> ToDictionary()
        {
            if (string.IsNullOrEmpty(ClientKey) || string.IsNullOrEmpty(ClientId))
            {
                throw new InvalidOperationException("Missing key");
            }

            return new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientKey,
                ["scope"] = Scope,
                ["grant_type"] = GrantType
            };
        }
    }
}
