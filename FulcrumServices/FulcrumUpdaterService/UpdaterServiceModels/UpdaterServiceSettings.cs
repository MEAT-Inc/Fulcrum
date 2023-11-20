using System;
using FulcrumEncryption;
using FulcrumService;
using FulcrumUpdaterService.JsonConverters;
using Newtonsoft.Json;

namespace FulcrumUpdaterService.UpdaterServiceModels
{
    /// <summary>
    /// Private class instance used to hold our injector configuration values for updates
    /// </summary>
    [JsonConverter(typeof(UpdaterServiceSettingsJsonConverter))]
    public class UpdaterServiceSettings : FulcrumServiceSettings
    {
        // Public properties which do not require encryption or decryption
        public bool ForceUpdateReady { get; set; }
        public string UpdaterOrgName { get; set; }
        public bool IncludePreReleases { get; set; }
        public TimeSpan RefreshTimerDelay { get; set; }
        public bool EnableAutomaticUpdates { get; set; }

        // Public properties which need to be decrypted or encrypted on conversion routines
        [EncryptedValue] public string UpdaterRepoName { get; set; }
        [EncryptedValue] public string UpdaterUserName { get; set; }
        [EncryptedValue] public string UpdaterSecretKey { get; set; }
    }
}
