using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSimulator.SimulationEvents;
using SharpWrapper;
using SharpWrapper.J2534Objects;

namespace FulcrumInjector.FulcrumViewContent.Models.SimulationModels
{
    /// <summary>
    /// Base class type for simulation event objects being processed around
    /// </summary>
    public class SimulationEventObject
    {
        // Date and time sent along with the sharp session in use
        public DateTime TimeProcessed { get; private set; }
        public readonly J2534Device SendingDevice;
        public readonly J2534Channel SendingChannel;
        public readonly Sharp2534Session SendingSharpSession;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Store values based on the input content type for a Message Event
        /// </summary>
        /// <param name="SimEvent"></param>
        public SimulationEventObject(SimMessageEventArgs SimEvent)
        {
            // Only populate if the session event is actually real
            this.SendingSharpSession = SimEvent.Session;
            this.SendingDevice = SimEvent.SessionDevice;
            this.SendingChannel = SimEvent.SessionChannel;

            // Set the time built
            this.TimeProcessed = DateTime.Now;
        }
        /// <summary>
        /// Store values based on the input content type for a Channel Event
        /// </summary>
        /// <param name="SimEvent"></param>
        public SimulationEventObject(SimChannelEventArgs SimEvent)
        { 
            // Only populate if the session event is actually real
            this.SendingSharpSession = SimEvent.Session;
            this.SendingDevice = SimEvent.SessionDevice;
            this.SendingChannel = SimEvent.SessionChannel;

            // Set the time built
            this.TimeProcessed = DateTime.Now;
        }

        /// <summary>
        /// Builds a new base simulation event object from the given input argument type
        /// </summary>
        public SimulationEventObject(EventArgs SimEvent)
        {
            // Ensure the event type is either a channel event or a sim message event
            Type EventType = SimEvent.GetType();
            if (EventType != typeof(SimMessageEventArgs) && EventType != typeof(SimChannelEventArgs))
                throw new InvalidCastException($"CAN NOT CAST EVENT TYPE OF {EventType.FullName} FOR SIMULATION MESSAGE EVENTS!");

            // Store date and time sent out
            this.TimeProcessed = DateTime.Now;

            // Store content values here based on the type of event
            if (EventType == typeof(SimMessageEventArgs))
            {
                // Cast into the event for a messages and store values
                var MessageEvent = SimEvent as SimMessageEventArgs;
                if (MessageEvent == null) return;

                // Only populate if the session event is actually real
                this.SendingSharpSession = MessageEvent.Session;
                this.SendingDevice = MessageEvent.SessionDevice;
                this.SendingChannel = MessageEvent.SessionChannel;
            }
            else
            {
                // Cast into the event for a channel and store values
                var ChannelEvent = SimEvent as SimChannelEventArgs;
                if (ChannelEvent == null) return;

                // Only populate if the session event is actually real
                this.SendingSharpSession = ChannelEvent.Session;
                this.SendingDevice = ChannelEvent.SessionDevice;
                this.SendingChannel = ChannelEvent.SessionChannel;
            }
        }
    }
}
