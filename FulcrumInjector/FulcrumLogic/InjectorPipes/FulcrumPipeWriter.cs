using System;
using System.IO;
using System.IO.Pipes;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.InjectorPipes
{
    /// <summary>
    /// Fulcrum pipe writing class. Sends data out to our DLLs
    /// </summary>
    public sealed class FulcrumPipeWriter : FulcrumPipe
    {
        // Singleton configuration
        public static FulcrumPipeWriter PipeInstance => _lazyWriter.Value;
        private static readonly Lazy<FulcrumPipeWriter> _lazyWriter = new(() => new FulcrumPipeWriter());

        // Reset Pipe Object method
        public static void ResetPipeInstance()
        {
            // Reset Pipe here if needed and can
            if (PipeInstance == null) { return; }
            if (PipeInstance.PipeState != FulcrumPipeState.Connected) PipeInstance.ConfigureNewPipe();
            else { PipeInstance.PipeLogger.WriteLog("WRITER PIPE WAS ALREADY CONNECTED! NOT RECONFIGURING IT!", LogType.WarnLog); }
        }

        // ------------------------------------------------------------------------------------------------------------------------------

        // Pipe writer and stream objects
        internal StreamWriter PipeWriter;
        internal NamedPipeServerStream FulcrumPipe;

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new outbound pipe sender
        /// </summary>
        private FulcrumPipeWriter() : base(FulcrumPipeType.FulcrumPipeBravo)
        {
            // Build the pipe object here.
            this.FulcrumPipe = new NamedPipeServerStream(
                base.FulcrumPipeBravo,    // Name of the pipe host
                PipeDirection.Out         // Direction of the pipe host      
            );

            // Build our new pipe instance here.
            if (!this.ConfigureNewPipe()) this.PipeLogger.WriteLog("FAILED TO CONFIGURE NEW OUTPUT WRITER PIPE!", LogType.ErrorLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new pipe instance for our type provided.
        /// </summary>
        /// <returns></returns>
        internal override bool ConfigureNewPipe()
        {
            // Check if our DLL is open
            if (!FulcrumDllLoaded())
            {
                this.PipeLogger.WriteLog("WARNING: OUR FULCRUM DLL WAS NOT IN USE! THIS MEANS WE WON'T BE BOOTING OUR PIPE INSTANCES!", LogType.WarnLog);
                return false;
            }

            // Log ready for connection and send it.
            this.PipeState = FulcrumPipeState.Open;
            this.PipeLogger.WriteLog("PIPE SERVER STREAM HAS BEEN CONFIGURED! ATTEMPTING CONNECTION ON IT NOW...", LogType.WarnLog);
            this.PipeLogger.WriteLog("WAITING FOR 10 SECONDS BEFORE THE PIPES WILL TIMEOUT DURING THE CONNECTION ROUTINE", LogType.TraceLog);

            try
            {
                // Build pipe reading stream object
                this.FulcrumPipe.WaitForConnection();
                this.PipeState = FulcrumPipeState.Connected;
                this.PipeWriter = new StreamWriter(this.FulcrumPipe) { AutoFlush = true };
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
        }

        // ------------------------------------------------------------------------------------------------------------------------------
    }
}
