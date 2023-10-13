using FulcrumInjector.FulcrumViewSupport.FulcrumEncryption;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.DriveBrokerModels
{
    /// <summary>
    /// Class object used to define the JSON object of our google drive authorization
    /// </summary>
    [JsonConverter(typeof(DriveAuthJsonConverter))]
    internal class DriveAuthorization
    {
        // Public properties which do not require encryption or decryption
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("auth_uri")] public string AuthUri { get; set; }
        [JsonProperty("token_uri")] public string TokenUri { get; set; }
        [JsonProperty("universe_domain")] public string UniverseDomain { get; set; }
        [JsonProperty("auth_provider_x509_cert_url")] public string AuthProviderUrl { get; set; }

        // Public properties which need to be decrypted or encrypted on conversion routines
        [EncryptedValue][JsonProperty("client_id")] public string ClientId { get; set; }
        [EncryptedValue][JsonProperty("project_id")] public string ProjectId { get; set; }
        [EncryptedValue][JsonProperty("private_key")] public string PrivateKey { get; set; }
        [EncryptedValue][JsonProperty("client_email")] public string ClientEmail { get; set; }
        [EncryptedValue][JsonProperty("private_key_id")] public string PrivateKeyId { get; set; }
        [EncryptedValue][JsonProperty("client_x509_cert_url")] public string ClientCertUrl { get; set; }
    }
}
