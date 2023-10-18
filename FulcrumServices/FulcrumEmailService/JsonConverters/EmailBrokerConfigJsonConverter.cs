using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumEmailService.EmailServiceModels;
using FulcrumEncryption;
using FulcrumJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumEmailService.JsonConverters
{
    /// <summary>
    /// JSON Converter for the Fulcrum Email broker configuration object
    /// </summary>
    internal class EmailBrokerConfigJsonConverter : EncryptionJsonConverter<EmailBrokerConfiguration>
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields
        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// CTOR for this JSON converter. Defaults encryption to on for reading and writing JSON values
        /// </summary>
        public EmailBrokerConfigJsonConverter() : base(true) { }
        /// <summary>
        /// CTOR for this JSON converter. Allows us to specify encryption state
        /// </summary>
        /// <param name="UseEncryption">When true, output is encrypted</param>
        public EmailBrokerConfigJsonConverter(bool UseEncryption = true) : base(UseEncryption) { }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, EmailBrokerConfiguration ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is EmailBrokerConfigJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Build a dynamic output object using the properties of our email configuration
            // Encrypt "ReportSenderEmail", "ReportSenderPassword"
            var OutputObject = JObject.FromObject(new
            {
                ValueObject.ReportSenderName,
                ReportSenderEmail = this._useEncryption 
                    ? FulcrumEncryptor.Encrypt(ValueObject.ReportSenderName)
                    : ValueObject.ReportSenderName,
                ReportSenderPassword = this._useEncryption 
                    ? FulcrumEncryptor.Encrypt(ValueObject.ReportSenderPassword)
                    : ValueObject.ReportSenderPassword,
            });

            // Now write this built object and reset our encryption state if needed
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
            this._useEncryption = OriginalEncryptionState; 
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override EmailBrokerConfiguration ReadJson(JsonReader JReader, Type ObjectType, EmailBrokerConfiguration ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Pull our serializer settings and check for encryption if needed
            bool OriginalEncryptionState = this._useEncryption;
            List<JsonConverter> CustomConverters = JSerializer?.Converters.ToList() ?? new List<JsonConverter>();
            var ConfigConverter = CustomConverters.FirstOrDefault(ConvObj => ConvObj.GetType() == this.GetType());
            if (ConfigConverter is EmailBrokerConfigJsonConverter CastConverter) this._useEncryption = CastConverter._useEncryption;

            // Read in our properties for the JObject and build a configuration from them
            string ReportSenderName = InputObject[nameof(EmailBrokerConfiguration.ReportSenderName)].Value<string>();
            string ReportSenderEmail = InputObject[nameof(EmailBrokerConfiguration.ReportSenderEmail)].Value<string>();
            string ReportSenderPassword = InputObject[nameof(EmailBrokerConfiguration.ReportSenderPassword)].Value<string>();

            // Build a new output object using our pulled properties
            var OutputObject = new EmailBrokerConfiguration()
            {
                // Store the properties of our configuration here and exit out
                ReportSenderName = ReportSenderName,
                ReportSenderEmail = this._useEncryption
                    ? FulcrumEncryptor.Decrypt(ReportSenderEmail)
                    : ReportSenderEmail,
                ReportSenderPassword = this._useEncryption
                    ? FulcrumEncryptor.Decrypt(ReportSenderPassword)
                    : ReportSenderPassword,
            };

            // Reset our encryption state and return the built object
            this._useEncryption = OriginalEncryptionState;
            return OutputObject;
        }
    }
}
