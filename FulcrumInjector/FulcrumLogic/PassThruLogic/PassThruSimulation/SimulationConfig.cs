using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using Newtonsoft.Json;
using SharpSimulator;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonConverters;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation
{
    /// <summary>
    /// Configuration object for simulation configurations.
    /// These are pulled from JSON in settings file or built in real time
    /// </summary>
    [JsonConverter(typeof(SimulationConfigJsonConverter))]
    public class SimulationConfig
    {
        // Reader default configurations
        public uint ReaderTimeout;
        public uint ReaderMsgCount;

        // Basic Channel Configurations
        public uint ReaderBaudRate;
        public ProtocolId ReaderProtocol;
        public PassThroughConnect ReaderChannelFlags;

        // Reader configuration filters and IOCTLs
        public J2534Filter[] ReaderFilters; 
        public PassThruStructs.SConfigList ReaderConfigs;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Pulls out a configuration template for a given protocol value
        /// </summary>
        /// <param name="ProtocolToUse">Protocol to pull</param>
        /// <returns>Built configuration object for the given protocol or null if none exist</returns>
        public static SimulationConfig BuildConfigForProtocol(ProtocolId ProtocolToUse)
        {
            // Find the full list of configurations first then find one with the name we want
            var ConfigurationsFound = ValueLoaders.GetConfigValue<SimulationConfig[]>("FulcrumSimConfigurations");
            return ConfigurationsFound.FirstOrDefault(ConfigObj => ConfigObj.ReaderProtocol == ProtocolToUse);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new configuration object and sets defaults to null/empty
        /// </summary>
        public SimulationConfig(ProtocolId ProtocolInUse, uint BaudRate)
        {
            // Store protocol and BaudRate
            this.ReaderBaudRate = BaudRate;
            this.ReaderProtocol = ProtocolInUse;

            // Store basic values here
            this.ReaderMsgCount = 1;
            this.ReaderTimeout = 100;
            this.ReaderChannelFlags = 0x00;

            // Setup basic empty array for filters with a max count of 10
            this.ReaderFilters = new J2534Filter[10];
            this.ReaderConfigs = new PassThruStructs.SConfigList(0);
        }
    }
}
