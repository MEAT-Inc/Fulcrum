using FulcrumInjector.FulcrumViewSupport.FulcrumEncryption;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumModels.EmailBrokerModels
{
    /// <summary>
    /// Class object holding the definition for the email broker configuration
    /// </summary>
    [JsonConverter(typeof(EmailBrokerConfigJsonConverter))]
    internal class EmailBrokerConfiguration
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public properties which do not require encryption or decryption
        public string ReportSenderName { get; set; }
        public string DefaultReportRecipient { get; set; }

        // Public properties which need to be decrypted or encrypted on conversion routines
        [EncryptedValue] public string ReportSenderEmail { get; set; }
        [EncryptedValue] public string ReportSenderPassword { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
