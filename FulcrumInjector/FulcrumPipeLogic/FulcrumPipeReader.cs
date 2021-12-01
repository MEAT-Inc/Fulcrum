using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using FulcrumInjector.FulcrumLogging.LoggerSupport;

namespace FulcrumInjector.FulcrumPipeLogic
{
    /// <summary>
    /// Pipe reading instance for our fulcrum server
    /// </summary>
    public class FulcrumPipeReader : FulcrumPipe
    {
        // Pipe and reader objects for data
        internal readonly StreamReader PipeReader;
        internal readonly NamedPipeClientStream FulcrumPipe;

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new reading pipe instance for Fulcrum
        /// </summary>
        public FulcrumPipeReader() : base(FulcrumPipeType.FulcrumPipeAlpha)
        {
            // Build the pipe object here.
            this.FulcrumPipe = new NamedPipeClientStream(
                ".",                   // Name of the pipe host
                base.FulcrumPipeAlpha,          // Name of the pipe client
                PipeDirection.In,               // Pipe directional configuration
                PipeOptions.Asynchronous,       // Pipe operational modes
                TokenImpersonationLevel.None    // Token spoofing mode is set to none.
            );

            // Log ready for connection and send it.
            this.PipeState = FulcrumPipeState.Open;
            this.PipeLogger.WriteLog("PIPE CLIENT STREAM HAS BEEN CONFIGURED! ATTEMPTING CONNECTION ON IT NOW...", LogType.WarnLog);
            this.PipeLogger.WriteLog("WAITING FOR 10 SECONDS BEFORE THE PIPES WILL TIMEOUT DURING THE CONNECTION ROUTINE", LogType.TraceLog);

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
                this.PipeLogger.WriteLog($"FAILED TO CONNECT TO OUR PIPE INSTANCE FOR PIPE ID {this.PipeType}!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING CONNECTION OR STREAM OPERATIONS FOR THIS PIPE CONFIGURATION!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW", PipeEx);
            }
        }
    }
}
