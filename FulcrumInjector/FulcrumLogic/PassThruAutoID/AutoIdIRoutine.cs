using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumLogic.PassThruAutoID.AutoIdModels;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.PassThruTypes;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumLogic.PassThruAutoID
{
    /// <summary>
    /// Interface base for Auto ID routines which can be used by our connection routine.
    /// This interface lays out a Open, Connect, Read VIN, and Close command.
    /// </summary>
    public abstract class AutoIdRoutine
    {
        // Logger object for monitoring logger outputs
        protected internal readonly SubServiceLogger AutoIdLogger;

        // Class Values for configuring commands.
        public readonly string DLL;
        public readonly string Device;
        public readonly JVersion Version;
        public readonly ProtocolId AutoIdType;
        public readonly AutoIdModels.AutoIdRoutine AutoIdCommands;

        // Runtime Instance Values (private only)
        protected internal uint[] FilterIds;
        protected internal uint ChannelIdOpened;
        protected internal Sharp2534Session SessionInstance;

        // Result values from our instance.
        public string VinNumberLocated { get; protected set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new connection instance for AutoID
        /// </summary>
        protected internal AutoIdRoutine(JVersion ApiVersion, string DllName, string DeviceName, ProtocolId ProtocolValue)
        {
            // Store class values here and build our new logger object.
            this.DLL = DllName;
            this.Device = DeviceName;
            this.Version = ApiVersion;
            this.AutoIdType = ProtocolValue;

            // Build our new logger object
            string LoggerName = $"{ProtocolValue}_AutoIdLogger_{this.Version}_{DeviceName.Replace(" ", "-")}";
            this.AutoIdLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith(LoggerName)) ?? new SubServiceLogger(LoggerName);

            // Log built new auto ID routine without issues.
            this.AutoIdLogger.WriteLog($"BUILT NEW AUTO ID LOGGER FOR PROTOCOL {this.AutoIdType} OK!", LogType.InfoLog);
            this.AutoIdLogger.WriteLog($"--> DLL IN USE:    {this.DLL}");
            this.AutoIdLogger.WriteLog($"--> DEVICE IN USE: {this.Device}");

            // Build our AutoID routine object from the AppSettings now.
            this.AutoIdLogger.WriteLog($"PULLING IN SESSION ROUTINES FOR PROTOCOL TYPE {this.AutoIdType}", LogType.InfoLog);
            var SupportedProtocols = ValueLoaders.GetConfigValue<string[]>("FulcrumAutoIdRoutines.SupportedProtocols")
                .Select(ProcString => Enum.TryParse(ProcString, out ProtocolId PulledProtocol) ? PulledProtocol : 0).ToArray();
            if (!SupportedProtocols.Contains(this.AutoIdType)) throw new InvalidOperationException($"CAN NOT USE PROTOCOL TYPE {this.AutoIdType} FOR AUTO ID ROUTINE!");

            // JSON Parse our input objects
            var SupportedRoutines = ValueLoaders.GetConfigValue<object[]>("FulcrumAutoIdRoutines.CommandRoutines").Select(InputObj =>
            {
                // Convert into JSON here.
                string ObjectString = JsonConvert.SerializeObject(InputObj);
                AutoIdModels.AutoIdRoutine RoutineObject = (AutoIdModels.AutoIdRoutine)JsonConvert.DeserializeObject(ObjectString, typeof(AutoIdModels.AutoIdRoutine));
                this.AutoIdLogger.WriteLog($"--> BUILT NEW SETTINGS ROUTINE OBJECT FOR PROTOCOL {RoutineObject.AutoIdType} OK!", LogType.InfoLog);
                return RoutineObject;
            });

            // Store our auto ID type routine
            this.AutoIdCommands = SupportedRoutines.FirstOrDefault(RoutineObj => RoutineObj.AutoIdType == this.AutoIdType);
            if (this.AutoIdCommands == null) throw new NullReferenceException($"FAILED TO FIND AUTO ID ROUTINE COMMANDS FOR PROTOCOL {this.AutoIdType}!");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens a new session for J2534 sessions.
        /// </summary>
        /// <param name="DllName">Name of DLL</param>
        /// <param name="DeviceName">Name of Device</param>
        /// <param name="SessionInstance">Instance built</param>
        /// <returns>True if the session is built ok. False if it is not.</returns>
        public bool OpenSession(Sharp2534Session InputSession = null)
        {
            try
            {
                // Store our instance session
                this.SessionInstance = InputSession ?? new Sharp2534Session(this.Version, this.DLL, this.Device);
                this.AutoIdLogger.WriteLog("STORED INSTANCE SESSION OK! READY TO BEGIN AN AUTO ID ROUTINE WITH IT NOW...");

                // Open our session object and begin connecting
                this.SessionInstance.PTOpen();
                this.AutoIdLogger.WriteLog("BUILT NEW SHARP SESSION FOR ROUTINE OK! SHOWING RESULTS BELOW", LogType.InfoLog);
                this.AutoIdLogger.WriteLog(this.SessionInstance.ToDetailedString());

                // Now connect our channel object
                if (this.ConnectChannel(out this.ChannelIdOpened)) this.AutoIdLogger.WriteLog("CONNECTED TO OUR CHANNEL INSTANCE OK!", LogType.InfoLog);
                else throw new InvalidOperationException($"FAILED TO CONNECT TO NEW {this.AutoIdType} CHANNEL!");
                return true;
            }
            catch (Exception SessionEx)
            {
                // Log our exception and throw failures.
                this.AutoIdLogger.WriteLog($"FAILED TO BUILD AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this.AutoIdLogger.WriteLog("EXCEPTION THROWN DURING SESSION CONFIGURATION METHOD", SessionEx);
                return false;
            }
        }
        /// <summary>
        /// Closes our session for building an AutoID routine.
        /// </summary>
        /// <returns>True if the session was closed ok. False if not.</returns>
        public bool CloseSession()
        {
            try
            {
                // Start by issuing a PTClose method.
                this.SessionInstance.PTDisconnect(0);
                this.AutoIdLogger.WriteLog("CLOSED SESSION INSTANCE OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception SessionEx)
            {
                // Log our exception and throw failures.
                this.AutoIdLogger.WriteLog($"FAILED TO CLOSE AN AUTO ID ROUTINE SESSION FOR PROTOCOL TYPE {this.AutoIdType}!", LogType.ErrorLog);
                this.AutoIdLogger.WriteLog("EXCEPTION THROWN DURING SESSION SHUTDOWN METHOD", SessionEx);
                return false;
            }
        }

        /// <summary>
        /// Connects to a given channel instance using the protocol value given in the class type and the 
        /// </summary>
        /// <param name="ChannelId">Channel ID Opened</param>
        /// <returns>True if the channel is opened, false if it is not.</returns>
        public abstract bool ConnectChannel(out uint ChannelId);
        /// <summary>
        /// Finds the VIN of the currently connected vehicle
        /// </summary>
        /// <param name="VinNumber">VIN Number pulled</param>
        /// <returns>True if a VIN is pulled, false if it isn't</returns>
        public abstract bool RetrieveVinNumber(out string VinNumber);
    }
}
