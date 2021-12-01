using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;

namespace FulcrumInjector.FulcrumPipeLogic
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
    public class FulcrumPipe
    {
        // Fulcrum Logger. Build this once the pipe is built.
        internal readonly SubServiceLogger PipeLogger;

        // State of the pipe reading client object
        protected FulcrumPipeState _pipeState;
        public FulcrumPipeState PipeState
        {
            get => _pipeState;
            protected set
            {
                this._pipeState = value;
                PipeLogger?.WriteLog($"PIPE {this.PipeType} STATE IS NOW: {this._pipeState}", LogType.TraceLog);
            }
        }

        // Pipe Configurations for the default values.
        public readonly string FulcrumPipeAlpha = "2CC3F0FB08354929BB453151BBAA5A15";
        public readonly string FulcrumPipeBravo = "1D16333944F74A928A932417074DD2B3";

        // Pipe configuration information.
        public readonly string PipeLocation;
        public readonly FulcrumPipeType PipeType;

        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new fulcrum pipe listener
        /// </summary>
        /// <param name="PipeId">ID Of the pipe in use for this object</param>
        protected FulcrumPipe(FulcrumPipeType PipeId)
        {
            // Configure logger object.
            this.PipeState = FulcrumPipeState.Faulted;
            this.PipeLogger = new SubServiceLogger($"{PipeId}");
            this.PipeLogger.WriteLog($"BUILT NEW PIPE LOGGER FOR PIPE TYPE {PipeId} OK!", LogType.InfoLog);

            // Store information about the pipe being configured
            this.PipeType = PipeId;
            this.PipeLocation = this.PipeType == FulcrumPipeType.FulcrumPipeAlpha ? FulcrumPipeAlpha : FulcrumPipeBravo;
            this.PipeLogger.WriteLog("STORED NEW PIPE DIRECTIONAL INFO AND TYPE ON THIS INSTANCE CORRECTLY!", LogType.InfoLog);
        }
    }
}
