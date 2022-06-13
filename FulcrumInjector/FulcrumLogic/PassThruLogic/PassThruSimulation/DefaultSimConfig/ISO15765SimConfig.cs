using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSimulator.SimulationObjects;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation.DefaultSimConfig
{
    /// <summary>
    /// Default configuration for ISO15765 11 BIT simulation sessions
    /// </summary>
    public class ISO15765SimConfig
    {
        // Start by building a new simulation channel object to load in.
        public static readonly uint ReaderBaudRate = 500000;
        public static readonly uint ReaderChannelFlags = 0x00;
        public static readonly ProtocolId ReaderProtocol = ProtocolId.ISO15765;
       
        // ------------------------------------------------------------------------------------------------------------------------------------------

        // Reader configuration filters and IOCTLs
        public static readonly J2534Filter[] ReaderFilters = new[]
        {
            // Passing all 0x07 0xXX Addresses
            new J2534Filter()
            {
                FilterFlags = 0x00,
                FilterFlowCtl = "",
                FilterMask = "00 00 07 00",
                FilterPattern = "00 00 07 00",
                FilterProtocol = ProtocolId.CAN,
                FilterType = FilterDef.PASS_FILTER,
            },

            // Blocking out the 0x07 0x72 address (Used for testing on my GM Moudle)
            new J2534Filter()
            {
                FilterFlags = 0x00,
                FilterFlowCtl = "",
                FilterMask = "00 00 07 72",
                FilterPattern = "00 00 07 72",
                FilterProtocol = ProtocolId.CAN,
                FilterType = FilterDef.BLOCK_FILTER,
            },
        };
        public static readonly PassThruStructs.SConfigList ReaderConfigs = new PassThruStructs.SConfigList
        {
            // Number of configs being setup
            NumberOfParams = 1,
            ConfigList = new List<PassThruStructs.SConfig>()
            {
                // CAN Mixed format configuration
                new PassThruStructs.SConfig()
                {
                    SConfigParamId = ConfigParamId.CAN_MIXED_FORMAT,
                    SConfigValue = 1
                },
            }
        };
    }
}
