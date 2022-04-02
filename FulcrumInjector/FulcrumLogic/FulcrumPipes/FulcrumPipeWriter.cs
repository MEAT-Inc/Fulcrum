using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.FulcrumPipes
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
        public static bool InitializePipeInstance(out Task<bool> ConnectionTask)
        {
            // Store default Task value
            ConnectionTask = null;

            // Reset Pipe here if needed and can
            if (PipeInstance == null) { return false; }
            if (PipeInstance.PipeState != FulcrumPipeState.Connected) { PipeInstance.AttemptPipeConnection(out ConnectionTask); return true; } 
            else { PipeInstance.PipeLogger.WriteLog("WRITER PIPE WAS ALREADY CONNECTED! NOT RECONFIGURING IT!", LogType.WarnLog); return false; }
        }

        // ------------------------------------------------------------------------------------------------------------------------------

        // Sets if we can run a new connection or not
        public static bool IsConnecting { get; private set; }

        // Pipe writer and stream objects
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

            // Build event helper for state changed.
            this.PipeStateChanged += (PipeObj, SendingArgs) =>
            {
                // Check if currently connecting or not.
                if (IsConnecting) return;
                if (IsConnecting || SendingArgs.NewState != FulcrumPipeState.Open) return;
                
                // Now run the connection routine and wait for results
                this.PipeLogger.WriteLog("DETECTED A NEW STATE OF OPEN FOR OUR PIPE WRITER! TRYING TO CONNECT IT NOW...", LogType.WarnLog);
                this.StartAsyncConnectClient();
            };

            // Build our new pipe instance here.
            if (this.AttemptPipeConnection(out _)) return;

            // Log failures and return output
            this.PipeLogger.WriteLog("FAILED TO CONFIGURE NEW OUTPUT WRITER PIPE!", LogType.ErrorLog);
            this.PipeLogger.WriteLog("PIPE FAILURE WILL BE MONITORED AND CONNECTIONS WILL BE RETRIED ON A PRESET INTERVAL...", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new pipe instance for our type provided.
        /// </summary>
        /// <returns></returns>
        internal override bool AttemptPipeConnection(out Task<bool> ConnectionTask)
        {
            try
            {
                // Set state value. Get new clients
                this.PipeState = FulcrumPipeState.Open;
                this.PipeLogger.WriteLog("STARTING WRITER PIPE CONNECTION ROUTINE NOW. THIS MAY TAKE A BIT...", LogType.WarnLog);
                
                // Run the routine here and log information for setup
                ConnectionTask = this.StartAsyncConnectClient();
                this.PipeLogger.WriteLog("PIPE HOST WRITER STREAM HAS BEEN CONFIGURED! ATTEMPTING TO FIND CLIENTS FOR IT NOW...", LogType.WarnLog);
                this.PipeLogger.WriteLog($"WAITING FOR NEW CLIENT ENDLESSLY BEFORE BREAKING OUT OF SETUP METHODS!", LogType.WarnLog);
                return true;
            }
            catch (Exception PipeEx)
            {
                // Log failed to connect to our pipe.
                ConnectionTask = null;
                this.PipeState = FulcrumPipeState.Faulted;
                this.PipeLogger.WriteLog($"FAILED TO CONNECT TO OUR PIPE INSTANCE FOR PIPE ID {this.PipeType}!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING CONNECTION OR STREAM OPERATIONS FOR THIS PIPE CONFIGURATION!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS BEING LOGGED BELOW", PipeEx);

                // Start async updating
                return false;
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------------


        /// <summary>
        /// Async connects to our client on the reader side of operations
        /// </summary>
        private Task<bool> StartAsyncConnectClient()
        {
            // Check if connected already or not
            if (this.FulcrumPipe.IsConnected || IsConnecting)
            {
                // Check what condition we hit
                this.PipeLogger.WriteLog(
                    IsConnecting
                        ? "CAN NOT FORCE A NEW CONNECTION ATTEMPT WHILE A PREVIOUS ONE IS ACTIVE!"
                        : "PIPE WAS ALREADY CONNECTED! RETURNING OUT NOW...", LogType.WarnLog);
                
                // Exit this method
                return null;
            }

            // Apply it based on values pulled and try to open a new client
            Stopwatch ConnectionTimeStopwatch = new Stopwatch();
            this.PipeLogger.WriteLog("STARTING WRITER PIPE CONNECTION ROUTINE NOW...", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Run a task while the connected value is false
                IsConnecting = true;
                ConnectionTimeStopwatch.Start();
                this.FulcrumPipe.WaitForConnection();

                // If we're connected, log that information and break out
                this.PipeState = FulcrumPipeState.Connected;
                this.PipeLogger.WriteLog("CONNECTED NEW CLIENT INSTANCE!", LogType.WarnLog);
                this.PipeLogger.WriteLog($"PIPE CLIENT CONNECTED TO FULCRUM PIPER {this.PipeType} OK!", LogType.InfoLog);
                this.PipeLogger.WriteLog($"ESTIMATED {ConnectionTimeStopwatch.ElapsedMilliseconds} MILLISECONDS ELAPSED FOR CLIENT CONNECTION!", LogType.WarnLog);
                IsConnecting = false;
                return true;
            });
        }
    }
}
