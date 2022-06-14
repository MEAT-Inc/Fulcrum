using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.JsonLogic.JsonConverters
{
    /// <summary>
    /// JSON converter for reading and saving Simulation configuration objects
    /// </summary>
    public class SimulationConfigJsonConverter : JsonConverter
    {       
        /// <summary>
        /// Determines if we can convert an object or not.
        /// </summary>
        /// <param name="ObjectType">Type input</param>
        /// <returns>True if conversion works, false if not.</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(SimulationConfig); }

        /// <summary>
        /// Writes a new JSON Object output
        /// </summary>
        /// <param name="JWriter"></param>
        /// <param name="ValueObject"></param>
        /// <param name="JSerializer"></param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if object is null. Build output
            if (ValueObject == null) { return; }
            SimulationConfig ConfigObjectCast = ValueObject as SimulationConfig;

            // Build dynamic filter list
            var FiltersConverted = ConfigObjectCast.ReaderFilters.Select(FilterObj => new
            {
                FilterObj.FilterFlags,
                FilterObj.FilterMask,
                FilterObj.FilterPattern,
                FilterObj.FilterFlowCtl,
                FilterProtocol = FilterObj.FilterProtocol.ToString(),
                FilterType = FilterObj.FilterType.ToString()
            });

            // Build dynamic Configs list
            var ConfigsConverted = ConfigObjectCast.ReaderConfigs.ConfigList.Select(ConfigObj => new
            {
                SConfigParamId = ConfigObj.SConfigParamId.ToString(),
                SConfigValue = ConfigObj.SConfigValue
            });

            // Build full JSON object to write out
            JObject OutputObject = JObject.FromObject(new
            {
                ConfigObjectCast.ReaderTimeout,
                ConfigObjectCast.ReaderMsgCount,
                ConfigObjectCast.ReaderBaudRate,
                ConfigObjectCast.ReaderChannelFlags,
                ReaderFilters = FiltersConverted,
                ReaderConfigs = ConfigsConverted
            });

            // Write the built object
            JWriter.WriteRaw(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads JSON in from a JReader object
        /// </summary>
        /// <param name="JReader"></param>
        /// <param name="ObjectType"></param>
        /// <param name="ExistingValue"></param>
        /// <param name="JSerializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }
            
            // Get timeout and read count values here
            uint Timeout = InputObject["ReaderTimeout"].Value<uint>();
            uint MsgCount = InputObject["ReaderMsgCount"].Value<uint>();

            // Get Channel configuration values here
            uint BaudRate = InputObject["ReaderBaudRate"].Value<uint>();
            ProtocolId ReaderProtocol = InputObject["ReaderProtocol"].Type == JTokenType.String ?
                (ProtocolId)Enum.Parse(typeof(ProtocolId), InputObject["ReaderProtocol"].Value<string>()) :
                (ProtocolId)InputObject["ReaderProtocol"].Value<uint>();

            // Find the channel flags here
            PassThroughConnect ChannelFlags = InputObject["ReaderChannelFlags"].Type == JTokenType.String ?
                (PassThroughConnect)Enum.Parse(typeof(TxFlags), InputObject["ReaderChannelFlags"].Value<string>()) :
                (PassThroughConnect)InputObject["ReaderChannelFlags"].Value<uint>();

            // Get our filter objects here
            var FiltersPulled = InputObject["ReaderFilters"].Value<JArray>();
            J2534Filter[] FiltersBuilt = (J2534Filter[])FiltersPulled.Select(FilterObj => new J2534Filter()
            {
                // Store filter values here
                FilterFlags = FilterObj["FilterFlags"].Value<uint>(),
                FilterMask = FilterObj["FilterMask"].Value<string>(),
                FilterPattern = FilterObj["FilterMask"].Value<string>(),
                FilterFlowCtl = FilterObj["FilterMask"].Value<string>(),
                FilterType = FilterObj["FilterType"].Type == JTokenType.String ?
                    (FilterDef)Enum.Parse(typeof(FilterDef), FilterObj["FilterType"].Value<string>()) :
                    (FilterDef)FilterObj["FilterType"].Value<uint>(),
                FilterProtocol = FilterObj["FilterProtocol"].Type == JTokenType.String ?
                    (ProtocolId)Enum.Parse(typeof(ProtocolId), FilterObj["FilterProtocol"].Value<string>()) :
                    (ProtocolId)FilterObj["FilterProtocol"].Value<uint>()
            });

            // Build our SConfig List here
            var ConfigListItems = InputObject["ReaderConfigs"]["ConfigList"].Value<JArray>();
            PassThruStructs.SConfig[] ConfigItemsCast = (PassThruStructs.SConfig[])ConfigListItems.Select(ConfigObj => new PassThruStructs.SConfig()
            {
                // Set value for the config ID and the value
                SConfigValue = ConfigObj["SConfigValue"].Value<uint>(),
                SConfigParamId = ConfigObj["SConfigParamId"].Type == JTokenType.String ?
                    (ConfigParamId)Enum.Parse(typeof(ConfigParamId), ConfigObj["SConfigParamId"].Value<string>()) :
                    (ConfigParamId)ConfigObj["SConfigParamId"].Value<uint>()
            });

            // Build output SConfig List
            PassThruStructs.SConfigList ConfigsBuilt = new PassThruStructs.SConfigList((uint)ConfigItemsCast.Length) { ConfigList = ConfigItemsCast.ToList() };

            // Now build the configuration object
            SimulationConfig OutputConfig = new SimulationConfig(ReaderProtocol, BaudRate)
            {
                ReaderMsgCount = MsgCount,
                ReaderTimeout = Timeout,
                ReaderBaudRate = BaudRate,
                ReaderChannelFlags = ChannelFlags,
                ReaderFilters = FiltersBuilt,
                ReaderConfigs = ConfigsBuilt
            };

            // Return the output object
            return OutputConfig;
        }
    }
}
