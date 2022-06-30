using System.Linq;
using SharpSimulator.SimulationEvents;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumViewContent.Models.SimulationModels
{
    /// <summary>
    /// Builds a new object which is bound onto for our Message event display on the simulation view
    /// </summary>
    public class SimMessageEventObject : SimulationEventObject
    {
        // Message Content Values
        public bool ResponsePassed { get; private set; }
        public string MessageReadString { get; private set; }
        public string[] MessageResponseStrings { get; private set; }

        // Message Objects themselves
        public PassThruStructs.PassThruMsg ReadMessage { get; private set; }
        public PassThruStructs.PassThruMsg[] SentMessages { get; private set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new event model from the given event arguments
        /// </summary>
        public SimMessageEventObject(SimMessageEventArgs EventArgs) : base(EventArgs)
        {
            // Store message content values here
            this.ResponsePassed = EventArgs.ResponsePassed;
            this.MessageReadString = EventArgs.ReadMessage.DataToHexString();
            string[] ContentMessages = EventArgs.Responses.Select(RespMsg => RespMsg.DataToHexString()).ToArray();
            if (!ResponsePassed) ContentMessages = ContentMessages.Prepend("Responses Failed!").ToArray();
            this.MessageResponseStrings = ContentMessages;

            // Store raw message objects here
            this.ReadMessage = EventArgs.ReadMessage;
            this.SentMessages = EventArgs.Responses;
        }
    }
}
