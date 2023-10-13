using FulcrumInjector.FulcrumViewSupport.FulcrumEncryption;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels
{
    /// <summary>
    /// Private class instance used to hold our injector configuration values for updates
    /// </summary>
    [JsonConverter(typeof(UpdaterConfigJsonConverter))]
    public class FulcrumUpdaterConfiguration
    {
        // Public properties which do not require encryption or decryption
        public bool ForceUpdateReady { get; set; }
        public string UpdaterOrgName { get; set; }

        // Public properties which need to be decrypted or encrypted on conversion routines
        [EncryptedValue] public string UpdaterRepoName { get; set; }
        [EncryptedValue] public string UpdaterUserName { get; set; }
        [EncryptedValue] public string UpdaterSecretKey { get; set; }
    }

}
