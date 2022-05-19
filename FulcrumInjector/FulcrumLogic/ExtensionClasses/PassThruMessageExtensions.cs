using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpWrap2534.PassThruTypes;

namespace FulcrumInjector.FulcrumLogic.ExtensionClasses
{
    /// <summary>
    /// Extensions for PassThru messages
    /// </summary>
    public static class PassThruMessageExtensions
    {
        /// <summary>
        /// Pulls in a new PassThru message and converts it from a flow control message to a normal CAN Message
        /// </summary>
        /// <param name="FlowControlMessage">Message to convert</param>
        /// <returns>Array of flow control messages converted to CAN</returns>
        public static PassThruStructs.PassThruMsg[] ToCanArray(this PassThruStructs.PassThruMsg FlowControlMessage)
        {
            // Check if we can convert this message
            if (FlowControlMessage.ProtocolID != ProtocolId.ISO15765 || FlowControlMessage.Data.Length == 0)
                return new PassThruStructs.PassThruMsg[] { FlowControlMessage };

            // This method starts by pulling out the data of our message and finding out how many parts we need to split it into
            // Sample Input Data:
            //      0x00 0x00 0x07 0xE8 0x49 0x02 0x01 0x31 0x47 0x31 0x46 0x42 0x33 0x44 0x53 0x33 0x4B 0x30 0x31 0x31 0x37 0x32 0x32 0x38
            //
            // Using the input message size (24 in this case) we need to built N Number of messages with a max size of 12 bytes for each one
            // We also need to include the changes needed to make sure the message count bytes are included
            // So with that in mind, our 24 bytes of data gets 3 bytes removed and included in our first message out
            // First message value would be as follows
            //      00 00 07 E8 10 14 49 02 01 31 47 31
            //          00 00 07 E8 -> Response Address
            //          10 14 -> Indicates a multiple part message and the number of bytes left to read
            //          49 02 -> 09 + 40 means positive response and 02 is the command issues
            //          01 -> Indicates data begins
            //          31 47 31 -> First three bytes of data
            // 
            // Then all following messages follow the format of 00 00 0X XX TC DD DD DD DD DD DD DD
            //      00 00 0X -> Padding plus the address start byte
            //      XX -> Address byte two
            //      TC -> T - Total messages and C - Current Message number
            //      DD DD DD DD DD DD DD -> Data of the message value
            // 
            // We also need to include a frame pad indicator output. This message is just 00 00 07 and  the input address byte two 
            // minus 8. So for 00 00 07 E8, we would get 00 00 07 E0
            //
            // This means for our input message value in this block of text, our output must look like the following
            //      00 00 07 E8 10 14 49 02 01 31 47 31     --> Start of message and first three bytes
            //      00 00 07 E0 00 00 00 00 00 00 00 00     --> Frame pad message
            //      00 00 07 E8 21 46 42 33 44 53 33 4B     --> Bytes 4-10
            //      00 00 07 E8 22 30 31 31 37 32 32 38     --> Bytes 11-17

            return null;
        }
        /// <summary>
        /// Converts a CAN input array to a flow control message
        /// </summary>
        /// <param name="CanArrayInput">Array of CAN to convert</param>
        /// <returns>The Built output message</returns>
        public static PassThruStructs.PassThruMsg ToFlowControl(this PassThruStructs.PassThruMsg[] CanArrayInput)
        {
            // Check if we can actually run this routine or not.
            if (CanArrayInput.Length == 0) return default;
            throw new NotImplementedException("CONVERTING FROM CAN ARRAYS TO FLOW CONTROL IS NOT YET BUILT!");
        }
    }
}
