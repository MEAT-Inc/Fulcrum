using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogging.LoggerSupport;

namespace FulcrumInjector.FulcrumPipeLogic
{
    /// <summary>
    /// Fulcrum pipe writing class. Sends data out to our DLLs
    /// </summary>
    public class FulcrumPipeWriter : FulcrumPipe
    {
        // Pipe writer and stream objects
        internal readonly StreamWriter PipeWriter;
        internal readonly NamedPipeServerStream FulcrumPipe;

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new outbound pipe sender
        /// </summary>
        public FulcrumPipeWriter() : base(FulcrumPipeType.FulcrumPipeBravo)
        {
            // Build the pipe object here.
            this.FulcrumPipe = new NamedPipeServerStream(
                base.FulcrumPipeBravo,    // Name of the pipe host
                PipeDirection.Out         // Direction of the pipe host      
            );

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
