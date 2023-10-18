namespace FulcrumEmailService.EmailServiceModels
{
    /// <summary>
    /// Class object holding the definition for the email broker SMTP Configuration
    /// </summary>
    public class EmailSmtpConfiguration
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing properties holding configuration for the broker SMTP setup
        public int ServerPort { get; set; }
        public int ServerTimeout { get; set; }
        public string ServerName { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes
    }
}
