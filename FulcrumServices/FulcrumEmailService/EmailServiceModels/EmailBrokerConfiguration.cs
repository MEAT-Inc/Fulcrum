using FulcrumEmailService.JsonConverters;
using FulcrumEncryption;
using Newtonsoft.Json;

namespace FulcrumEmailService.EmailServiceModels
{
    /// <summary>
    /// Class object holding the definition for the email broker configuration
    /// </summary>
    [JsonConverter(typeof(EmailBrokerConfigJsonConverter))]
    public class EmailBrokerConfiguration
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public properties which do not require encryption or decryption
        public string ReportSenderName { get; set; }
        public bool IncludeInjectorLog { get; set; }
        public bool IncludeServiceLogs { get; set; }
        public string DefaultEmailBodyText { get; set; }
        public string[] DefaultReportRecipients { get; set; }

        // Public properties which need to be decrypted or encrypted on conversion routines
        [EncryptedValue] public string ReportSenderEmail { get; set; }
        [EncryptedValue] public string ReportSenderPassword { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
