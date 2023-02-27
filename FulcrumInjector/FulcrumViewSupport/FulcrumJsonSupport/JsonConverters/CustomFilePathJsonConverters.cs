using System;
using System.IO;
using Newtonsoft.Json;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport.JsonConverters
{
    /// <summary>
    /// Removes all info from a file path except the last value of the variable path
    /// </summary>
    internal class FilePathValueSerializer : JsonConverter
    {
        /// <summary>
        /// Clean out the JSON Values from the property given
        /// </summary>
        /// <param name="JWriter">Writer object</param>
        /// <param name="ObjectValue">Value of object</param>
        /// <param name="SerializerObject">Serializer object</param>
        public override void WriteJson(JsonWriter JWriter, object ObjectValue, JsonSerializer SerializerObject)
        {
            // Clean out path and write it
            string FilePath = (string)ObjectValue;
            string PathCleanedUp = new FileInfo(FilePath).Name;

            // Write the value out
            JWriter.WriteValue(PathCleanedUp);
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
            // Throw this exception since reading is not supported
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }
        /// <summary>
        /// Sets if this object can be converted.
        /// </summary>
        /// <param name="ObjectType">Type of object</param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType)
        {
            // Returns if the type of the object to convert is a string
            return ObjectType == typeof(string);
        }
    }
}
