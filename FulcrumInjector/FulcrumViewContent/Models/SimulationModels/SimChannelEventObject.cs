using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSimulator.SimulationEvents;
using SharpWrapper.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.Models.SimulationModels
{
    /// <summary>
    /// Event object output built for a simulation channel event being processed
    /// </summary>
    public class SimChannelEventObject : SimulationEventObject
    {
        // Content Values for new Channel
        public uint ChannelId { get; private set; }
        public BaudRate ChannelBaud { get; private set; }
        public ProtocolId ChannelProtocol { get; private set; }

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
