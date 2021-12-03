using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumPipeLogic
{
    /// <summary>
    /// Pipe reading instance for our fulcrum server
    /// </summary>
    public class FulcrumPipeReader : FulcrumPipe
    {
        // Pipe and reader objects for data
        internal StreamReader PipeReader;
        internal NamedPipeClientStream FulcrumPipe;

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

            // Build our new pipe instance here.
            if (!this.ConfigureNewPipe()) this.PipeLogger.WriteLog("FAILED TO CONFIGURE NEW OUTPUT WRITER PIPE!", LogType.ErrorLog);
        }

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new pipe instance for our type provided.
        /// </summary>
        /// <returns>True if the pipe was built OK. False if not.</returns>
        internal sealed override bool ConfigureNewPipe()
        {
            // Check if our DLL is open
            if (!FulcrumDllLoaded())
            {
                this.PipeLogger.WriteLog("WARNING: OUR FULCRUM DLL WAS NOT IN USE! THIS MEANS WE WON'T BE BOOTING OUR PIPE INSTANCES!", LogType.WarnLog);
                return false;
            }
            
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
                return true;
            }
            catch (Exception PipeEx)
            {
                // Log failed to connect to our pipe.
                this.PipeState = FulcrumPipeState.Faulted;
                this.PipeLogger.WriteLog($"FAILED TO CONNECT TO OUR PIPE INSTANCE FOR PIPE ID {this.PipeType}!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING CONNECTION OR STREAM OPERATIONS FOR THIS PIPE CONFIGURATION!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW", PipeEx);
                return false;
            }

            // -------------------------------------------------------------------------------------------------------
        }
    }
}
