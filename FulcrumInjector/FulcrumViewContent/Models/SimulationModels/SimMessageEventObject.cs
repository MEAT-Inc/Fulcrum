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
        public readonly bool ResponsePassed;
        public readonly string MessageReadString;
        public readonly string[] MessageResponseStrings;

        // Message Objects themselves
        public readonly PassThruStructs.PassThruMsg ReadMessage;
        public readonly PassThruStructs.PassThruMsg[] SentMessages;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new event model from the given event arguments
        /// </summary>
        public SimMessageEventObject(SimMessageEventArgs EventArgs) : base(EventArgs)
        {
            // Store message content values here
            this.ResponsePassed = EventArgs.ResponsePassed;
            this.MessageReadString = EventArgs.ReadMessage.DataToHexString();
            this.MessageResponseStrings = this.ResponsePassed ?
                EventArgs.Responses.Select(RespMsg => RespMsg.DataToHexString()).ToArray() :
                new[] { "Responses Failed!", "No Content Output Responses" };

            // Store raw message objects here
            this.ReadMessage = EventArgs.ReadMessage;
            this.SentMessages = EventArgs.Responses;
        }
    }
}
