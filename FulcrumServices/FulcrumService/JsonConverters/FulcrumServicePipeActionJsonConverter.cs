using System;
using System.Collections.Generic;
using System.Linq;
using FulcrumEncryption;
using FulcrumJson;
using FulcrumService.FulcrumServiceModels;
using FulcrumSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FulcrumService.JsonConverters
{
    /// <summary>
    /// JSON Converter for the FulcrumService Pipe Actions
    /// </summary>
    internal class FulcrumServicePipeActionJsonConverter : JsonConverter<FulcrumServicePipeAction>
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
        /// Writes JSON output for the given input object
        /// </summary>
        /// <param name="JWriter">The JWriter building output content for the input value</param>
        /// <param name="ValueObject">The object being written out to a JSON string</param>
        /// <param name="JSerializer">Serializer settings for the writer output</param>
        public override void WriteJson(JsonWriter JWriter, FulcrumServicePipeAction ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            
            // Build a dynamic output object using the properties of our pipe action object
            var OutputObject = JObject.FromObject(new
            {
                PipeActionGuid = ValueObject.PipeActionGuid.ToString("D").ToUpper(),
                PipeServiceType = ValueObject.PipeServiceType.ToDescriptionString(), 
                ReflectionType = ValueObject.ReflectionType.ToDescriptionString(),
                ValueObject.IsExecuted,
                PipeMethodName = ValueObject.PipeActionName,
                ValueObject.PipeMethodArguments,
                PipeArgumentTypes = ValueObject.PipeArgumentTypes.Select(ArgType => ArgType.AssemblyQualifiedName),
                ValueObject.PipeCommandResult
            });

            // Now write this built object and reset our encryption state if needed
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.None));
        }
        /// <summary>
        /// Reads the JSON object input from a string
        /// </summary>
        /// <param name="JReader">The JReader being used to read our input JSON content</param>
        /// <param name="ObjectType">The type of object we're trying to build form the input JSON</param>
        /// <param name="ExistingValue">An existing object to update values for based on our new object</param>
        /// <param name="HasExistingValue">Sets if there's an existing value we're updating or not</param>
        /// <param name="JSerializer">Serializer settings for the reader input</param>
        /// <returns>The object built from the input JSON content</returns>
        public override FulcrumServicePipeAction ReadJson(JsonReader JReader, Type ObjectType, FulcrumServicePipeAction ExistingValue, bool HasExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // Read in our properties for the JObject and build a pipe action from them
            bool IsExecuted = InputObject[nameof(FulcrumServicePipeAction.IsExecuted)].Value<bool>();
            string PipeMethodName = InputObject[nameof(FulcrumServicePipeAction.PipeActionName)].Value<string>();
            object PipeCommandResult = InputObject[nameof(FulcrumServicePipeAction.PipeCommandResult)].Value<object>();
            Guid PipeActionGuid = Guid.Parse(InputObject[nameof(FulcrumServicePipeAction.PipeActionGuid)].Value<string>());
            var PipeServiceType = (InputObject[nameof(FulcrumServicePipeAction.PipeServiceType)].Value<string>()).ToEnumValue<FulcrumServiceBase.ServiceTypes>();
            var ReflectionType = (InputObject[nameof(FulcrumServicePipeAction.ReflectionType)].Value<string>()).ToEnumValue<FulcrumServicePipeAction.ReflectionTypes>();

            // Build our list of argument type values here
            JArray TypesJArray = JArray.FromObject(InputObject[nameof(FulcrumServicePipeAction.PipeArgumentTypes)]);
            List<Type> ArgumentTypes = TypesJArray.Select(TypeToken => Type.GetType(TypeToken.ToString())).ToList();

            // Build a list of argument objects from our input JSON content here
            int TokenIndex = 0; List<object> BuiltActionArgs = new List<object>();
            JArray ArgumentJArray = JArray.FromObject(InputObject[nameof(FulcrumServicePipeAction.PipeMethodArguments)]);
            foreach (var ArgumentToken in ArgumentJArray)
            {
                // If the object in the array is not an array or list, then just add it to our collection objects
                if (ArgumentToken.Type != JTokenType.Object && ArgumentToken.Type != JTokenType.Array)
                {
                    // Add this argument and tick our token counter value
                    BuiltActionArgs.Add(ArgumentToken.ToObject(ArgumentTypes[TokenIndex]));
                    TokenIndex++;
                    continue;
                }
                
                // Convert our object into the desired type, store it, and tick our token counter 
                object ConvertedArgumentObject = ArgumentToken.ToObject(ArgumentTypes[TokenIndex]);
                BuiltActionArgs.Add(ConvertedArgumentObject);
                TokenIndex++;
            }

            // Build a new output object using our pulled properties
            var OutputObject = new FulcrumServicePipeAction()
            {
                IsExecuted = IsExecuted,
                PipeActionGuid = PipeActionGuid,
                PipeActionName = PipeMethodName,
                ReflectionType = ReflectionType,
                PipeServiceType = PipeServiceType,
                PipeCommandResult = PipeCommandResult,
                PipeArgumentTypes = ArgumentTypes.ToArray(),
                PipeMethodArguments = BuiltActionArgs.ToArray(),
            };

            // Reset our encryption state and return the built object
            return OutputObject;
        }
    }
}
