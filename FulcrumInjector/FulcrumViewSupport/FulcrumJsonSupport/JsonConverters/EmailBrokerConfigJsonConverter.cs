using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using static FulcrumInjector.FulcrumViewSupport.FulcrumEmailBroker;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// JSON Converter for the Fulcrum Email broker configuration object
    /// </summary>
    internal class EmailBrokerConfigJsonConverter : JsonConverter
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties

        // Public facing property used to determine if we encrypt the output content of our JSON object or not
        private readonly bool _useEncryption;

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this JSON converter. Defaults encryption to on for reading and writing JSON values
        /// </summary>
        public EmailBrokerConfigJsonConverter()
        {
            // Store our encryption configuration state 
            this._useEncryption = true;
        }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public EmailBrokerConfigJsonConverter(bool UseEncryption = true)
        {
            // Store our encryption configuration state 
            this._useEncryption = UseEncryption;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(EmailBrokerConfiguration); }
        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            EmailBrokerConfiguration EmailConfig = ValueObject as EmailBrokerConfiguration;

            // Build a dynamic output object using the properties of our email configuration
            // Encrypt "ReportSenderEmail", "ReportSenderPassword"
            var OutputObject = JObject.FromObject(new
            {
                EmailConfig.ReportSenderName,
                ReportSenderEmail = this._useEncryption 
                    ? StringEncryptor.Encrypt(EmailConfig.ReportSenderName)
                    : EmailConfig.ReportSenderName,
                ReportSenderPassword = this._useEncryption 
                    ? StringEncryptor.Encrypt(EmailConfig.ReportSenderPassword)
                    : EmailConfig.ReportSenderPassword,
            });

            // Now write this built object.
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Read in our properties for the JObject and build a configuration from them
            string ReportSenderName = InputObject[nameof(EmailBrokerConfiguration.ReportSenderName)].Value<string>();
            string ReportSenderEmail = InputObject[nameof(EmailBrokerConfiguration.ReportSenderEmail)].Value<string>();
            string ReportSenderPassword = InputObject[nameof(EmailBrokerConfiguration.ReportSenderPassword)].Value<string>();

            // Return built output object
            return new EmailBrokerConfiguration()
            {
                // Store the properties of our configuration here and exit out
                ReportSenderName = ReportSenderName,
                ReportSenderEmail = this._useEncryption 
                    ? StringEncryptor.Decrypt(ReportSenderEmail)
                    : ReportSenderEmail,
                ReportSenderPassword = this._useEncryption 
                    ? StringEncryptor.Decrypt(ReportSenderPassword)
                    : ReportSenderPassword,
            };
        }
    }
}
