using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSimulator.SimulationEvents;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.Models.SimulationModels
{
    /// <summary>
    /// Event object output built for a simulation channel event being processed
    /// </summary>
    public class SimChannelEventObject : SimulationEventObject
    {
        // Content Values for new Channel
        public readonly uint ChannelId;
        public readonly BaudRate ChannelBaud;
        public readonly ProtocolId ChannelProtocol;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new Sim Channel event object based on an input channel event argument set
        /// </summary>
        /// <param name="SimEvent"></param>
        public SimChannelEventObject(SimChannelEventArgs SimEvent) : base(SimEvent)
        {
            // Store content values here.
            this.ChannelId = SimEvent.SessionChannel.ChannelId;
            this.ChannelProtocol = SimEvent.SessionChannel.ProtocolId;
            this.ChannelBaud = (BaudRate)SimEvent.SessionChannel.ChannelBaud;
        }
    }
}
