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
            var FilterFlags = uint.Parse(FilterContent[0][4]);
            var FilterType = FilterExpression.FilterType.Split(':')[1];
            var FilterProtocol = (ProtocolId)uint.Parse(FilterContent[0][2].Split(':')[0]);
            var FilterPatten = FilterContent[0].Last().Replace("0x ", string.Empty);
            var FilterMask = FilterContent[1].Last().Replace("0x ", string.Empty);
            var FilterFlow = FilterContent.Count != 3 ? "" : FilterContent[2].Last().Replace("0x ", string.Empty);

            // Now convert our information into string values.
            return new J2534Filter()
            {
                // Build a new filter object form the given values and return it.
                FilterMask = FilterMask,
                FilterFlags = FilterFlags,
                FilterProtocol = FilterProtocol,
                FilterPattern = Inverted ? FilterFlow : FilterPatten,
                FilterFlowCtl = Inverted ? FilterPatten : FilterFlow,
                FilterType = (FilterDef)Enum.Parse(typeof(FilterDef), FilterType)
            };
        }
        /// <summary>
        /// Converts a PTWrite Message Expression into a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg ConvertWriteExpression(PassThruWriteMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return default;
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg MessageBuilt = default;
            foreach (var MessageSet in MessageContents)
            {
                // Store message values here.
                var MessageData = MessageSet.Last();
                var MessageFlags = uint.Parse(MessageSet[3]);
                var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[4].Split(':')[0]);

                // TODO: FORMAT THIS CODE TO WORK FOR DIFFERENT PROTOCOL VALUE TYPES!

                // ISO15765 11 Bit
                if (MessageData.StartsWith("00 00"))      
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
                    string[] FormattedData = MessageDataSplit.Take(4).ToArray();                           // 00 00 07 DF
                    string[] DataSentOnly = MessageDataSplit.Skip(4).ToArray();                            // 01 00
                    string[] FinalData = FormattedData
                        .Concat(new string[] { "0x" + DataSentOnly.Length.ToString("X") })
                        .Concat(DataSentOnly)
                        .ToArray();                                                                        // 00 00 07 DF 02 01 00                                                           
                    string[] TrailingZeros = Enumerable.Repeat("0x00", 12 - FinalData.Length).ToArray();
                    FinalData = FinalData.Concat(TrailingZeros).ToArray();                                 // 00 00 07 DF 02 01 00 00 00 00 00

                    // Convert back into a string value and format
                    MessageData = string.Join(" ", FinalData);
                    MessageData = MessageData.Replace("0x", string.Empty);
                }

                // ISO15765 29 Bit
                if (MessageData.StartsWith("18 db"))
                {
                    // TODO: BUILD FORMATTING ROUTINE FOR 29 BIT CAN!
                }

                // Build our final output message.
                MessageBuilt = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
            }

            // Return the built message
            return MessageBuilt;
        }
        /// <summary>
        /// Converts an input PTRead Expression to a PTMessage
        /// </summary>
        /// <param name="MessageExpression"></param>
        /// <returns></returns>
        public static PassThruStructs.PassThruMsg ConvertReadExpression(PassThruReadMessagesExpression MessageExpression)
        {
            // Store the Message Data and the values of the message params.
            MessageExpression.FindMessageContents(out List<string[]> MessageContents);
            if (MessageContents.Count == 0)
            {
                MessageExpression.ExpressionLogger.WriteLog("MESSAGE CONTENTS WERE NOT ABLE TO BE EXTRACTED!", LogType.ErrorLog);
                MessageExpression.ExpressionLogger.WriteLog($"MESSAGE COMMAND LINES ARE SHOWN BELOW:\n{MessageExpression.CommandLines}", LogType.TraceLog);
                return default;
            }

            // Loop all the message values located and append them into the list of output
            PassThruStructs.PassThruMsg BuiltMessage = default;
            foreach (var MessageSet in MessageContents)
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
                var MessageFlags = uint.Parse(MessageSet[4]);
                var ProtocolId = (ProtocolId)Enum.Parse(typeof(ProtocolId), MessageSet[2].Split(':')[0]);

                // Build a message and then return it.
                MessageData = MessageData.Replace("0x", string.Empty);
                BuiltMessage = J2534Device.CreatePTMsgFromString(ProtocolId, MessageFlags, MessageData);
            }

            // Return the message
            return BuiltMessage;
        }
    }
}
