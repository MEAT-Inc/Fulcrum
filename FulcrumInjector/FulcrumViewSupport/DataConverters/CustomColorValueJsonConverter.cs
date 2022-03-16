using System;
using System.Drawing;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.DataConverters
{
    public class CustomColorValueJsonConverter : JsonConverter
    {
        /// <summary>
        /// Clean out the JSON Values from the property given
        /// </summary>
        /// <param name="JWriter">Writer object</param>
        /// <param name="ObjectValue"></param>
        /// <param name="SerializerObject"></param>
        public override void WriteJson(JsonWriter JWriter, object ObjectValue, JsonSerializer SerializerObject)
        {
            // If not a color value, convert this to an object and write it out
            if (ObjectValue.GetType() != typeof(Color)) { JWriter.WriteValue(JsonConvert.SerializeObject(ObjectValue)); return; }

            // Generate the color string value
            Color CastColor = (Color)ObjectValue;
            string ColorHexString = CustomColorConverter.HexConverter(CastColor);

            // Write the value
            JWriter.WriteValue(ColorHexString);
        }
        /// <summary>
        /// Not used. Read command.
        /// </summary>
        /// <param name="JReader">Json Reader</param>
        /// <param name="ObjectType">Type of object</param>
        /// <param name="ExistingValue">Value of object</param>
        /// <param name="SerializerObject">Serializer object</param>
        /// <returns></returns>
        public override object ReadJson(JsonReader JReader, Type ObjectType, object ExistingValue, JsonSerializer SerializerObject)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }
        /// <summary>
        /// Bool to set if the value is readable or not.
        /// </summary>
        public override bool CanRead => false;
        /// <summary>
        /// Sets if this object can be converted.
        /// </summary>
        /// <param name="ObjectType">Type of object</param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType)
        {
            return ObjectType == typeof(string);
        }
    }
}
