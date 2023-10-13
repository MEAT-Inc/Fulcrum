using FulcrumInjector.FulcrumViewSupport.FulcrumEncryption;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.DriveBrokerModels
{
    /// <summary>
    /// Class object used to define the JSON object of a google drive explorer configuration
    /// </summary>
    [JsonConverter(typeof(DriveConfigJsonConverter))]
    internal class DriveConfiguration
    {
        // Public properties which do not require encryption or decryption
        [JsonProperty("auth_uri")] public string AuthUri { get; set; }
        [JsonProperty("token_uri")] public string TokenUri { get; set; }
        [JsonProperty("redirect_uris")] public string[] RedirectUris { get; set; }
        [JsonProperty("auth_provider_x509_cert_url")] public string AuthProvider { get; set; }

        // Public properties which need to be decrypted or encrypted on conversion routines
        [EncryptedValue][JsonProperty("client_id")] public string ClientId { get; set; }
        [EncryptedValue][JsonProperty("project_id")] public string ProjectId { get; set; }
        [EncryptedValue][JsonProperty("client_secret")] public string ClientSecret { get; set; }
    }
}
