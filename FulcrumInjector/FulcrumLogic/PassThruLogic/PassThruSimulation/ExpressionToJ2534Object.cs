using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruExpressions.ExpressionObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic.PassThruSimulation
{
    /// <summary>
    /// Static class used to convert expressions into J2534 objects
    /// </summary>
    public class ExpressionToJ2534Object
    {
        /// <summary>
        /// Converts a J2534 Filter expression into a filter object
        /// </summary>
        /// <param name="FilterExpression"></param>
        /// <returns></returns>
        public static J2534Filter ConvertFilterExpression(PassThruStartMessageFilterExpression FilterExpression, bool Inverted = false)
        {
            // Store the Pattern, Mask, and Flow Ctl objects if they exist.
            FilterExpression.FindFilterContents(out List<string[]> FilterContent);
            if (FilterContent.Count == 0)
            {
                FilterExpression.ExpressionLogger.WriteLog("FILTER CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                FilterExpression.ExpressionLogger.WriteLog($"FILTER COMMAND LINES ARE SHOWN BELOW:\n{FilterExpression.CommandLines}", LogType.TraceLog);
                return null;
            }
            
            // Build filter output contents
            // BUG: NOT ALL EXTRACTED REGEX OUTPUT IS THE SAME! THIS RESULTS IN SOME POOR INDEXING ROUTINES
            try
            {
                var FilterType = FilterExpression.FilterType.Split(':')[1];
                var FilterFlags = (TxFlags)uint.Parse(FilterContent[0][4].Replace("TxF=", string.Empty));
                var FilterProtocol = (ProtocolId)uint.Parse(FilterContent[0][2].Split(':')[0]);
                var FilterPatten = FilterContent
                    .FirstOrDefault(FilterSet => FilterSet.Any(FilterString => FilterString.Contains("Pattern")))
                    .Last()
                    .Replace("0x ", string.Empty);
                var FilterMask = FilterContent
                    .FirstOrDefault(FilterSet => FilterSet.Any(FilterString => FilterString.Contains("Mask")))
                    .Last()
                    .Replace("0x ", string.Empty);
                var FilterFlow = 
                    FilterContent.Count != 3 ? "" : 
                    FilterContent.FirstOrDefault(FilterSet => FilterSet.Any(FilterString => FilterString.Contains("Flow")))
                        .Last()
                        .Replace("0x ", string.Empty);

                // Now convert our information into string values.
                FilterDef FilterTypeCast = (FilterDef)Enum.Parse(typeof(FilterDef), FilterType);
                J2534Filter OutputFilter = new J2534Filter()
                {
                    FilterFlags = FilterFlags, 
                    FilterProtocol = FilterProtocol, 
                    FilterType = FilterTypeCast, 
                    FilterStatus = PTInstanceStatus.INITIALIZED
                };
                
                // Now store the values for the message itself.
                if (FilterTypeCast == FilterDef.FLOW_CONTROL_FILTER)
                {
                    // Store a mask, pattern, and flow control value here
                    OutputFilter.FilterMask = FilterMask;
                    OutputFilter.FilterPattern = Inverted ? FilterFlow : FilterPatten;
                    OutputFilter.FilterFlowCtl = Inverted ? FilterPatten : FilterFlow;
                }
                else
                {
                    // Store ONLY a mask and a pattern here
                    OutputFilter.FilterMask = Inverted ? FilterPatten : FilterMask;
                    OutputFilter.FilterPattern = Inverted ? FilterMask : FilterPatten;
                    OutputFilter.FilterFlowCtl = string.Empty;
                }

                // Return the built J2534 filter object
                return OutputFilter;
            }
            catch (Exception ConversionEx)
            {
                // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                FilterExpression.ExpressionLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE FILTER! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                FilterExpression.ExpressionLogger.WriteLog($"FILTER EXPRESSION: {FilterExpression.CommandLines}", LogType.TraceLog);
                FilterExpression.ExpressionLogger.WriteLog("EXCEPTION THROWN", ConversionEx);
                return null;
            }
        }
        /// <summary>
        /// Converts a PTWrite Message Expression into a PTMessage
        /// Some additional info about this conversion routine
        ///
        /// This method starts by pulling out the data of our message and finding out how many parts we need to split it into
        /// Sample Input Data:
        ///      0x00 0x00 0x07 0xE8 0x49 0x02 0x01 0x31 0x47 0x31 0x46 0x42 0x33 0x44 0x53 0x33 0x4B 0x30 0x31 0x31 0x37 0x32 0x32 0x38
        ///
        /// Using the input message size (24 in this case) we need to built N Number of messages with a max size of 12 bytes for each one
        /// We also need to include the changes needed to make sure the message count bytes are included
        /// So with that in mind, our 24 bytes of data gets 3 bytes removed and included in our first message out
        /// First message value would be as follows
        ///      00 00 07 E8 10 14 49 02 01 31 47 31
        ///          00 00 07 E8 -> Response Address
        ///          10 14 -> Indicates a multiple part message and the number of bytes left to read
        ///          49 02 -> 09 + 40 means positive response and 02 is the command issues
        ///          01 -> Indicates data begins
        ///          31 47 31 -> First three bytes of data
        /// 
        /// Then all following messages follow the format of 00 00 0X XX TC DD DD DD DD DD DD DD
        ///      00 00 0X -> Padding plus the address start byte
        ///      XX -> Address byte two
        ///      TC -> T - Total messages and C - Current Message number
        ///      DD DD DD DD DD DD DD -> Data of the message value
        /// 
        /// We also need to include a frame pad indicator output. This message is just 00 00 07 and  the input address byte two 
        /// minus 8. So for 00 00 07 E8, we would get 00 00 07 E0
        ///
        /// This means for our input message value in this block of text, our output must look like the following
        ///      00 00 07 E8 10 14 49 02 01 31 47 31     --> Start of message and first three bytes
        ///      00 00 07 E0 00 00 00 00 00 00 00 00     --> Frame pad message
        ///      00 00 07 E8 21 46 42 33 44 53 33 4B     --> Bytes 4-10
        ///      00 00 07 E8 22 30 31 31 37 32 32 38     --> Bytes 11-17
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg[] ConvertWriteExpression(PassThruWriteMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                // Return an empty array of output objects
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg[] MessagesBuilt = Array.Empty<PassThruStructs.PassThruMsg>();
            foreach (var MessageSet in MessageContents)
            {
                // Wrap inside a try catch to ensure we get something back
                // TODO: FORMAT THIS CODE TO WORK FOR DIFFERENT PROTOCOL VALUE TYPES!
                try
                {
                    // Store message values here.
                    var MessageData = MessageSet.Last();
                    var MessageFlags = uint.Parse(MessageSet[3]);
                    var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[4].Split(':')[0]);

                    // ISO15765 11 Bit
                    if (MessageData.StartsWith("0x00 0x00"))
                    {
                        // 11 Bit messages need to be converted according to this format
                        // 00 00 07 DF 01 00 -- 00 00 07 DF 02 01 00 00 00 00 00 00
                        // Take first 4 bytes 
                        //      00 00 07 DF
                        // Count the number of bytes left
                        //      01 00 -- 2 bytes
                        // Insert the number of bytes (02) and the data sent
                        //      00 00 07 DF 02 01 00
                        // Take the number of bytes now and append 0s till it's 12 long
                        //      00 00 07 DF 02 01 00 00 00 00 00 00

                        // Build a message and then return it.
                        string[] MessageDataSplit = MessageData.Split(' ').ToArray();
                        string[] FormattedData = MessageDataSplit.Take(4).ToArray();                           // 00 00 07 DF   --   Padding + Send Address
                        string[] DataSentOnly = MessageDataSplit.Skip(4).ToArray();                            // 01 00         --   Actual data transmission
                        string[] FinalData = FormattedData
                            .Concat(new string[] { "0x" + DataSentOnly.Length.ToString("X2") })
                            .Concat(DataSentOnly)
                            .ToArray();                                                                        // 00 00 07 DF 02 01 00   --   Padding + Send Addr + Size + Data                                                         
                        string[] TrailingZeros = Enumerable.Repeat("0x00", 12 - FinalData.Length).ToArray();
                        FinalData = FinalData.Concat(TrailingZeros).ToArray();                                 // 00 00 07 DF 02 01 00 00 00 00 00 -- Finalized output message

                        // Convert back into a string value and format
                        MessageData = string.Join(" ", FinalData);
                        MessageData = MessageData.Replace("0x", string.Empty);
                    }

                    // ISO15765 29 Bit
                    if (MessageData.StartsWith("18 db")) {
                        // TODO: BUILD FORMATTING ROUTINE FOR 29 BIT CAN!
                    }

                    // Build our final output message.
                    var NextMessage = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
                    MessagesBuilt = MessagesBuilt.Append(NextMessage).ToArray();
                }
                catch (Exception ConversionEx)
                {
                    // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                    MessageExpression.ExpressionLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE MESSAGE! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                    MessageExpression.ExpressionLogger.WriteLog($"MESSAGE EXPRESSION: {MessageExpression.CommandLines}", LogType.TraceLog);
                    MessageExpression.ExpressionLogger.WriteLog("EXCEPTION THROWN", ConversionEx);
                    return default;
                }
            }

            // Return the built message
            return MessagesBuilt;
        }
        /// <summary>
        /// Converts an input PTRead Expression to a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg[] ConvertReadExpression(PassThruReadMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                // Return an empty array of output objects
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return Array.Empty<PassThruStructs.PassThruMsg>();
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg[] MessagesBuilt = Array.Empty<PassThruStructs.PassThruMsg>();
            foreach (var MessageSet in MessageContents)
            {
                // Wrap this in a try catch
                try
                {
                    // Store message values here.
                    var MessageData = MessageSet.Last();
                    if (MessageData.Contains("[") || MessageData.Contains("]"))
                    {
                        // Format for framepad output
                        MessageData = MessageData.Replace("0x", string.Empty);
                        string[] SplitData = MessageData
                            .Split(']')
                            .Select(SplitPart => SplitPart.Replace("[", string.Empty))
                            .Where(SplitPart => SplitPart.Length != 0)
                            .ToArray();

                        // Now restore message values
                        MessageData = string.Join(" ", SplitData);
                    }

                    // If it's not a frame pad message, add to our simulation
                    var RxStatus = uint.Parse(MessageSet[4].Replace("RxS=", string.Empty));
                    var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[2].Split(':')[0]);

                    // Build a message and then return it.
                    MessageData = MessageData.Replace("0x", string.Empty);
                    var NextMessage = J2534Device.CreatePTMsgFromString(ProtocolId, RxStatus, MessageData);
                    MessagesBuilt = MessagesBuilt.Append(NextMessage).ToArray();
                }
                catch (Exception ConversionEx)
                {
                    // TODO: FIND OUT WHY THIS ROUTINE CAN FAIL SOMETIMES!
                    MessageExpression.ExpressionLogger.WriteLog("FAILED TO CONVERT THIS EXPRESSION INTO A USABLE MESSAGE! EXCEPTION IS BEING SHOWN BELOW", LogType.WarnLog);
                    MessageExpression.ExpressionLogger.WriteLog($"MESSAGE EXPRESSION: {MessageExpression.CommandLines}", LogType.TraceLog);
                    MessageExpression.ExpressionLogger.WriteLog("EXCEPTION THROWN", ConversionEx);
                    return default;
                }
            }

            // Return the message
            return MessagesBuilt;
        }
    }
}
