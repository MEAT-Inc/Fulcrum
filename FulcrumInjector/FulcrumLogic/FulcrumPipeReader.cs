using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic
{
    /// <summary>
    /// Enums for pipe types
    /// </summary>
    public enum FulcrumPipeType
    {
        FulcrumPipeAlpha,      // Pipe number 1 (Input)
        FulcrumPipeBravo,      // Pipe number 2 (Output)
    }
    /// <summary>
    /// Possible states for our pipe objects.
    /// </summary>
    public enum FulcrumPipeState
    {
        Faulted,            // Failed to build
        Open,               // Open and not connected
        Connected,          // Connected
        Disconnected,       // Disconnected
        Closed,             // Open but closed manually
    }

    /// <summary>
    /// Instance object for reading pipe server data from our fulcrum DLL
    /// </summary>
    public class FulcrumPipeReader
    {
        // Fulcrum Logger. Build this once the pipe is built.
        private readonly SubServiceLogger PipeLogger;

        // State of the pipe reading client object
        private FulcrumPipeState _pipeState;
        public FulcrumPipeState PipeState
        {
            get => _pipeState;
            private set
            {
                this._pipeState = value;
                PipeLogger?.WriteLog($"PIPE {this.PipeType} STATE IS NOW: {this._pipeState}", LogType.TraceLog);
            }
        }

        // Pipe Configurations for the default values.
        private static readonly string FulcrumPipeAlpha = "2CC3F0FB08354929BB453151BBAA5A15";
        private static readonly string FulcrumPipeBravo = "1D16333944F74A928A932417074DD2B3";

        // Pipe configuration information.
        public readonly string PipeLocation;
        public readonly FulcrumPipeType PipeType;

        // Pipe and reader objects for data
        internal readonly StreamReader PipeReader;
        internal readonly NamedPipeClientStream FulcrumPipe;

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new fulcrum pipe listener
        /// </summary>
        /// <param name="PipeId">ID Of the pipe in use for this object</param>
        public FulcrumPipeReader(FulcrumPipeType PipeId)
        {
            // Configure logger object.
            this.PipeState = FulcrumPipeState.Faulted;
            this.PipeLogger = new SubServiceLogger($"FulcrumPipeLogger_{PipeId}");
            this.PipeLogger.WriteLog($"BUILT NEW PIPE LOGGER FOR PIPE TYPE {PipeId} OK!", LogType.InfoLog);

            // Store information about the pipe being configured
            this.PipeType = PipeId;
            this.PipeLocation = this.PipeType == FulcrumPipeType.FulcrumPipeAlpha ? FulcrumPipeAlpha : FulcrumPipeBravo;
            this.PipeLogger.WriteLog("STORED NEW PIPE DIRECTIONAL INFO AND TYPE ON THIS INSTANCE CORRECTLY!", LogType.InfoLog);

            // Build the pipe object here.
            this.FulcrumPipe = new NamedPipeClientStream(
                "FulcrumPipeHost",                             // Name of the pipe host
                this.PipeType.ToString(),                              // Name of the pipe client
                this.PipeType == FulcrumPipeType.FulcrumPipeAlpha ?    // Pipe directional configuration
                    PipeDirection.In :                                 //   --> Alpha is input 
                    PipeDirection.Out,                                 //   --> Bravo is output
                PipeOptions.Asynchronous,                              // Pipe operational modes
                TokenImpersonationLevel.None                           // Token spoofing mode is set to none.
            );

            // Log ready for connection and send it.
            this.PipeState = FulcrumPipeState.Open;
            this.PipeLogger.WriteLog("PIPE CLIENT STREAM HAS BEEN CONFIGURED! ATTEMPTING CONNECTION ON IT NOW...", LogType.WarnLog);
            this.PipeLogger.WriteLog("WAITING A TOTAL OF 10 SECONDS BEFORE THE PIPES WILL TIMEOUT DURING THE CONNECTION ROUTINE", LogType.TraceLog);

            try
            {
                // Build pipe reading stream object
                this.FulcrumPipe.Connect(2500);
                this.PipeState = FulcrumPipeState.Connected;
                this.PipeReader = new StreamReader(this.FulcrumPipe);
                this.PipeLogger.WriteLog("CONNECTED TO PIPE SERVER ON FULCRUM DLL CORRECTLY AND PULLED IN NEW STREAM FOR IT OK!", LogType.InfoLog);
            }
            catch (Exception PipeEx)
            {
                // Log failed to connect to our pipe.
                this.PipeState = FulcrumPipeState.Faulted;
                this.PipeLogger.WriteLog($"FAILED TO CONNECT TO OUR PIPE INSTANCE FOR PIPE ID {PipeId}!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING CONNECTION OR STREAM OPERATIONS FOR THIS PIPE CONFIGURATION!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW", PipeEx);
            } }
    }
}
