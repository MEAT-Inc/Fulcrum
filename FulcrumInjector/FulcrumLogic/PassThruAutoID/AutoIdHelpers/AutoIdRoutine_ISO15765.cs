using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.PassThruAutoID.AutoIdModels;
using Newtonsoft.Json;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.J2534Objects;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumLogic.PassThruAutoID.AutoIdHelpers
{
    /// <summary>
    /// Auto ID routine for an ISO15765 Routine configuration
    /// </summary>
    public class AutoIdRoutine_ISO15765 : AutoIdRoutine
    {
        /// <summary>
        /// Builds a new AutoID routine for ISO15765 channels
        /// </summary>
        /// <param name="ApiVersion">J2534 Version</param>
        /// <param name="DllName">DLL Name to use</param>
        /// <param name="DeviceName">Device Name to use</param>
        /// <param name="ProtocolValue">ProtocolValue to use</param>
        public AutoIdRoutine_ISO15765(Sharp2534Session InstanceSession) :
            base(InstanceSession.DeviceVersion, InstanceSession.DllName, InstanceSession.DeviceName, ProtocolId.ISO15765)
        {
            // Open the Session and store it here.
            this.OpenSession(this.SessionInstance);
            this.AutoIdLogger.WriteLog($"SETUP SESSION FOR INSTANCE PROTOCOL TYPE {this.AutoIdType} OK!", LogType.InfoLog);

            // Now open our channel and prepare to run commands. If this method passes, then we can just issue the connect/Vin pull method
            if (this.ConnectChannel(out this.ChannelIdOpened)) this.AutoIdLogger.WriteLog("CONNECTED TO OUR CHANNEL INSTANCE OK!", LogType.InfoLog);
            else throw new InvalidOperationException($"FAILED TO CONNECT TO NEW {this.AutoIdType} CHANNEL!");
        }
        /// <summary>
        /// Deconstruction for this instance object type.
        /// When this type is destroyed, we just run the close session method.
        /// </summary>
        ~AutoIdRoutine_ISO15765()
        {
            try
            {
                // Log information, close session.
                this.AutoIdLogger.WriteLog($"DISPOSING INSTANCE OF A {this.GetType().Name} AUTO ID ROUTINE INVOKER NOW...", LogType.WarnLog);
                if (this.CloseSession()) this.AutoIdLogger.WriteLog("CLOSED SESSION OK! RETURNING NOW...", LogType.InfoLog);
                else throw new InvalidOperationException("FAILED TO CLOSE SESSION INSTANCE DOWN FOR OUR AUTO ID ROUTINE! THIS IS ODD!");
            }
            catch (Exception CloseException)
            {
                // Log failures and return.
                this.AutoIdLogger.WriteLog($"FAILED TO CLOSE AND DISPOSE AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this.AutoIdLogger.WriteLog("EXCEPTION THROWN DURING SESSION DISPOSE METHOD", CloseException);
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Connect to our new ISO15765 Channel instance and issue filter commands.
        /// </summary>
        /// <param name="ChannelId">Channel ID Built</param>
        /// <returns></returns>
        public override bool ConnectChannel(out uint ChannelId)
        {
            // Connect to our new channel instance.
            this.SessionInstance.PTConnect(
                0,                                // Channel Index
                this.AutoIdType,                            // Type of protocol for scanning
                (uint)this.AutoIdCommands.ConnectFlags,     // Connection Flags
                (uint)this.AutoIdCommands.ConnectBaud,      // BaudRate value (ISO15765_50000)
                out ChannelId                               // Channel ID pulled from the open routine
            );

            // Log information about the newly issued command objects.
            this.AutoIdLogger.WriteLog($"ISSUES A PT CONNECT REQUEST FOR PROTOCOL {this.AutoIdType}!", LogType.InfoLog);
            this.AutoIdLogger.WriteLog($"CHANNEL ID OPENED WAS SEEN TO BE: {this.ChannelIdOpened}", LogType.WarnLog);

            // Build our filter objects here and apply them all.
            var FilterObjects = this.AutoIdCommands.RoutineFilters.Select(FilterObj =>
            {
                // Store our types for filters here.
                FilterDef FilterType = FilterObj.FilterType;
                ProtocolId FilterProtocol = FilterObj.FilterProtocol;
                string FilterMask = FilterObj.FilterMask.MessageData;
                string FilterPattern = FilterObj.FilterPattern.MessageData;
                string FilterFlowControl = FilterType == FilterDef.FLOW_CONTROL_FILTER ? FilterObj.FilterFlowControl.MessageData : null;

                try
                {
                    // Log filter details out and append it into our list of filters.
                    this.AutoIdLogger.WriteLog($"BUILDING FILTER WITH CONTENT: {JsonConvert.SerializeObject(FilterObj, Formatting.None)}", LogType.TraceLog);
                    var NewFilter = this.SessionInstance.PTStartMessageFilter(
                        FilterType,                         // Filter Definition
                        FilterMask,                         // Filter Mask
                        FilterPattern,                      // Filter Pattern
                        FilterFlowControl,                  // Filter Flow Control
                        (uint)FilterObj.FilterFlags,        // Filter Flags
                        (uint)FilterProtocol                // Filter Protocol
                    );

                    // Log new filter ID, append it into our filter list, and return it.
                    this.AutoIdLogger.WriteLog($"NEW FILTER OBJECT BUILT OK! FILTER ID IS: {NewFilter.FilterId}", LogType.InfoLog);
                    this.FilterIds = this.FilterIds.Append(NewFilter.FilterId).ToArray();

                    // Return the newly built filter object as an array here.
                    return NewFilter;
                }
                catch (Exception SetFilterEx)
                {
                    // Log failures, continue on.
                    this.AutoIdLogger.WriteLog($"FAILED TO SET NEW FILTER WITH TYPE {FilterObj.FilterType} FOR CHANNEL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                    this.AutoIdLogger.WriteLog("EXCEPTION THROWN DURING CHANNEL FILTER CONFIGURATION METHOD", SetFilterEx);
                    return null;
                }
            }).Where(FilterObj => FilterObj != null).ToArray();

            // Check to make sure we've got equal filter objects compared to input objects.
            if (FilterObjects.Length != this.AutoIdCommands.RoutineFilters.Length) 
                throw new InvalidOperationException("ERROR! FILTERS GENERATED DOES NOT MATCH THE COUNT OF FILTERS FED IN FROM APP JSON!");

            // Issue a Clear TX and RX buffer command here to setup all commands for reading/writing.
            this.SessionInstance.PTClearTxBuffer((int)ChannelId);
            this.SessionInstance.PTClearRxBuffer((int)ChannelId);
            this.AutoIdLogger.WriteLog("CLEARED OUT TX AND RX BUFFERS ON OUR SESSION DEVICE OK! READY TO READ AND WRITE NOW...", LogType.InfoLog);
            this.AutoIdLogger.WriteLog("SETUP ALL CHANNEL FILTERS OK! READY TO REQUEST A VIN NUMBER FROM THIS SESSION INSTANCE NOW...", LogType.InfoLog);
            
            // Return true since we setup ok and set the channel ID on the instance to our outward passed value generated from the PTConnect command.
            this.ChannelIdOpened = ChannelId;
            return true;
        }
        /// <summary>
        /// Finds the VIN of the currently connected vehicle using the newly opened ISO15765 Channel
        /// </summary>
        /// <param name="VinNumber">VIN Number pulled</param>
        /// <returns>True if a VIN is pulled, false if it isn't</returns>
        public override bool RetrieveVinNumber(out string VinNumber)
        {
            // Assign the VIN number a default blank value
            VinNumber = null;

            // Start by pulling in our message command contents. Assume the final command issued is the one which will return us a VIN Number.
            var MessageObjects = this.AutoIdCommands.RoutineCommands.Select(CommandObj =>
            {
                // Build a new PT Message from the content of this object
                var MessageBuilt = J2534Device.CreatePTMsgFromString(
                    CommandObj.MessageProtocol,
                    (uint)CommandObj.MessageFlags,
                    CommandObj.MessageData
                );

                // Log information, append this object into the list of new messages and move on.
                this.AutoIdLogger.WriteLog($"BUILT A NEW PASSTHRU COMMAND WITH MESSAGE CONTENT OF {CommandObj.MessageData} OK!", LogType.InfoLog);
                return MessageBuilt;
            }).ToArray();

            // Log information and the built messages here.
            this.AutoIdLogger.WriteLog("BUILT NEW PASSTHUR MESSAGES CORRECTLY! MESSAGES ARE SHOWN BELOW", LogType.InfoLog);
            this.AutoIdLogger.WriteLog(JsonConvert.SerializeObject(MessageObjects, Formatting.None));

            // Loop all our messages and send them out here.
            var ResponseMessages = new List<PassThruStructs.PassThruMsg>();
            foreach (var MessageObject in MessageObjects)
            {
                // Send the message, read back 10 messages, and wait for 250ms timeout.
                if (!this.SessionInstance.PTWriteMessages(MessageObject))
                    throw new Exception($"FAILED TO SEND MESSAGE ON {this.AutoIdType} CHANNEL!");

                // Print information about our sent message 
                string SentDataString = BitConverter.ToString(MessageObject.Data);
                this.AutoIdLogger.WriteLog($"SENT MESSAGE: {SentDataString}", LogType.InfoLog);

                // Read the message back here and store them on our class.
                var MessagesRead = this.SessionInstance.PTReadMessages(10, 250);
                this.AutoIdLogger.WriteLog($"READ IN A TOTAL OF {MessagesRead.Length} MESSAGES!", LogType.InfoLog);
                ResponseMessages.AddRange(MessagesRead);

                // Print out the message contents here.
                if (MessagesRead.Length == 0) this.AutoIdLogger.WriteLog("ERROR! NO MESSAGES WERE PROCESSED!", LogType.ErrorLog);
                else
                {
                    // Check if the message in use is null or not and print it
                    int MessageCount = 0;
                    foreach (var Message in MessagesRead)
                    {
                        // Build string value, print it out, tick our counter
                        var ReadDataString = string.Join(" ", BitConverter.ToString(Message.Data)
                            .Replace("-", " ")
                            .Split(' ')
                            .Select(MsgPart => $"0x{MsgPart}")
                            .ToArray()
                        );

                        // Print message contents here.
                        this.AutoIdLogger.WriteLog($"--> READ MESSAGE {MessageCount}: {ReadDataString}");
                    }
                }
            }

            // Now see if any of our messages are usable for our VIN Number.
            // 00 00 07 DF 49 02 01 XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX
            // Bytes 0-6 are for the response. 7-24 are our VIN Number values.
            var UsableMessages = ResponseMessages.Where(MsgObj => MsgObj.DataSize >= 24).ToArray();
            if (UsableMessages.Length == 0) throw new InvalidOperationException("NO USABLE VIN NUMBER RESPONSE WAS FOUND!");

            // Store our VIN Message, convert it to a string, and print it out
            var VinMessage = UsableMessages[0];
            string VINValue = Encoding.Default.GetString(VinMessage.Data.Skip(7).ToArray());
            this.AutoIdLogger.WriteLog($"VIN NUMBER VALUE PULLED: {VINValue}", LogType.InfoLog);
            this.VinNumberLocated = VINValue;
            return true;
        }
    }
}
