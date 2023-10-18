using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumEncryption;
using FulcrumService;

namespace FulcrumEmailService.EmailServiceModels
{
    /// <summary>
    /// Class object holding our configuration for the settings section to control an email service instance
    /// </summary>
    public class EmailServiceSettings : FulcrumServiceSettings
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing properties holding configuration for drive authorization
        public EmailSmtpConfiguration SmtpServerSettings { get; set; }
        public EmailBrokerConfiguration SenderConfiguration { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
