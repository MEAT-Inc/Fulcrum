using System;
using System.Drawing;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport
{
    /// <summary>
    /// Custom JSON Converter used to log out the theme style color values for the injector app
    /// </summary>
    internal class ColorValueJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert this object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're trying to convert</param>
        /// <returns>True if the object can be serialized, false if not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(string); }
        /// <summary>
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, object ValueObject, JsonSerializer JSerializer)
        {
            // If not a color value, convert this to an object and write it out
            if (ValueObject.GetType() != typeof(Color)) { JWriter.WriteValue(JsonConvert.SerializeObject(ValueObject)); return; }

            // Generate the color string value
            Color CastColor = (Color)ValueObject;
            string ColorHexString = CustomColorConverter.ToHexString(CastColor);

            // Write the value
            JWriter.WriteValue(ColorHexString);
        }
        /// <summary>
        /// NOT USED!
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override object ReadJson(JsonReader JReader, Type ObjectType, object ExistingValue, JsonSerializer SerializerObject)
        {
            // Throw this exception since reading is not supported
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }
    }
}
