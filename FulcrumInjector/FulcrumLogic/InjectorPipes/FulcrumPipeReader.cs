using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using FulcrumInjector.FulcrumLogic.InjectorPipes.PipeEvents;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.InjectorPipes
{
    /// <summary>
    /// Pipe reading instance for our fulcrum server
    /// </summary>
    public sealed class FulcrumPipeReader : FulcrumPipe
    {
        // Singleton configuration
        public static FulcrumPipeReader PipeInstance => _lazyReader.Value;
        private static readonly Lazy<FulcrumPipeReader> _lazyReader = new(() => new FulcrumPipeReader());

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

        // -------------------------------------------------------------------------------------------------------

        // Pipe and reader objects for data
        private NamedPipeClientStream FulcrumPipe;

        // Default value for pipe processing buffer
        private static int DefaultBufferValue = 10240;
        private static int DefaultReadingTimeout = 100;
        private static int DefaultConnectionTimeout = 10000;

        // Booleans to trigger operations
        private static bool IsReading = false;
        public static bool IsConnecting { get; private set; }

        // Task objects for monitoring background readers
        private CancellationTokenSource BackgroundRefreshSource;
        private CancellationTokenSource AsyncConnectionTokenSource;
        private CancellationTokenSource AsyncReadingOperationsTokenSource;

        // Event triggers for pipe data input
        public event EventHandler<FulcrumPipeDataReadEventArgs> PipeDataProcessed;
        protected void OnPipeDataProcessed(FulcrumPipeDataReadEventArgs EventArgs) { PipeDataProcessed?.Invoke(this, EventArgs); }

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new reading pipe instance for Fulcrum
        /// </summary>
        private FulcrumPipeReader() : base(FulcrumPipeType.FulcrumPipeAlpha)
        {
            // Build the pipe object here.
            this.FulcrumPipe = new NamedPipeClientStream(
                ".",                     // Name of the pipe host
                base.FulcrumPipeAlpha,           // Name of the pipe client
                PipeDirection.In,                // Pipe directional configuration
                PipeOptions.None                 // Async operations supported
            );

            // Store our new settings values for the pipe object and apply them
            DefaultBufferValue = InjectorConstants.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Buffer Size", DefaultBufferValue);
            DefaultReadingTimeout = InjectorConstants.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Processing Timeout", DefaultReadingTimeout);
            DefaultConnectionTimeout = InjectorConstants.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Connection Timeout", DefaultConnectionTimeout);
            this.PipeLogger.WriteLog($"STORED NEW DEFAULT BUFFER SIZE VALUE OK! VALUE STORED IS: {DefaultBufferValue}", LogType.WarnLog);
            this.PipeLogger.WriteLog($"STORED NEW CONNECTION TIMEOUT VALUE OK! VALUE STORED IS: {DefaultConnectionTimeout}", LogType.WarnLog);
            this.PipeLogger.WriteLog($"STORED NEW READ OPERATION TIMEOUT VALUE OK! VALUE STORED IS: {DefaultReadingTimeout}", LogType.WarnLog);

            // Build our new pipe instance here.
            if (this.AttemptPipeConnection(out _)) return;

            // Log failed to open and return if failed
            this.PipeState = FulcrumPipeState.Faulted;
            this.PipeLogger.WriteLog("FAILED TO CONFIGURE NEW INPUT READER PIPE!", LogType.ErrorLog);
            this.PipeLogger.WriteLog("PIPE FAILURE WILL BE MONITORED AND CONNECTIONS WILL BE RETRIED ON A PRESET INTERVAL...", LogType.WarnLog);
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new pipe instance for our type provided.
        /// </summary>
        /// <returns>True if the pipe was built OK. False if not.</returns>
        internal override bool AttemptPipeConnection(out Task<bool> ConnectionTask)
        {
            // Make sure the pipes aren't open
            if (this.FulcrumPipe.IsConnected) {
                ConnectionTask = null; return true;
            }

            try
            {
                // Log ready for connection and send it.
                this.PipeState = FulcrumPipeState.Open;
                ConnectionTask = this.StartAsyncHostConnect();
                this.PipeLogger.WriteLog("PIPE CLIENT STREAM HAS BEEN CONFIGURED! ATTEMPTING CONNECTION ON IT NOW...", LogType.WarnLog);
                this.PipeLogger.WriteLog($"WAITING FOR {DefaultConnectionTimeout} MILLISECONDS BEFORE THE PIPES WILL TIMEOUT DURING THE CONNECTION ROUTINE", LogType.TraceLog);
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
                return false;
            }
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Async connects to our client on the reader side of operations
        /// </summary>
        private Task<bool> StartAsyncHostConnect()
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

            // Build task token objects
            this.AsyncConnectionTokenSource = new CancellationTokenSource();
            this.PipeLogger.WriteLog("CONFIGURED NEW ASYNC CONNECTION TASK TOKENS OK!", LogType.InfoLog);
            DefaultConnectionTimeout = InjectorConstants.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Connection Timeout", DefaultConnectionTimeout);

            // Set connection building bool value and update view if possible
            IsConnecting = true;
            this.PipeState = FulcrumPipeState.Open;
            this.PipeLogger.WriteLog("STARTING READER PIPE CONNECTION ROUTINE NOW...", LogType.WarnLog);
            return Task.Run(() =>
            {
                // Build pipe reading stream object
                while (!this.FulcrumPipe.IsConnected)
                {
                    try { this.FulcrumPipe.Connect(DefaultConnectionTimeout); }
                    catch (Exception ConnectEx)
                    {
                        // Connecting to false
                        IsConnecting = false;
                        if (ConnectEx is not TimeoutException)
                        {
                            // Throw exception and return out assuming window content has been built now
                            this.PipeState = FulcrumPipeState.Faulted;
                            if (InjectorConstants.InjectorMainWindow != null) throw ConnectEx;
                            throw new Exception("FAILED TO CONFIGURE PIPE READER DUE TO NULL MAIN WINDOW INSTANCE! IS THE APP RUNNING?", ConnectEx);
                        }

                        // Set state to disconnected. Log failure
                        this.PipeState = FulcrumPipeState.Disconnected;
                        this.PipeLogger.WriteLog("FAILED TO CONNECT TO HOST PIPE SERVER AFTER GIVEN TIMEOUT VALUE!", LogType.WarnLog);
                        continue;
                    }

                    // If we're connected, log that information and break out
                    IsConnecting = false;
                    this.PipeState = FulcrumPipeState.Connected;
                    this.PipeLogger.WriteLog("CONNECTED NEW SERVER INSTANCE TO OUR READER!", LogType.WarnLog);
                    this.PipeLogger.WriteLog($"PIPE SERVER CONNECTED TO FULCRUM PIPER {this.PipeType} OK!", LogType.InfoLog);

                    // Now boot the reader process.
                    this.StartBackgroundReadProcess();
                }

                // Return passed once done
                return true;
            }, this.AsyncConnectionTokenSource.Token);
        }
        /// <summary>
        /// Kills our reading process if the process is currently running
        /// </summary>
        /// <returns></returns>
        internal bool StopAsyncConnectionProcess()
        {
            // Check if the source or token are null
            if (this.AsyncConnectionTokenSource == null) {
                this.PipeLogger.WriteLog("TOKENS AND SOURCES WERE NOT YET CONFIGURED WHICH MEANS CONNECTING WAS NOT STARTED!", LogType.WarnLog);
                return false;
            }

            // Cancel here and return
            this.PipeLogger.WriteLog("CANCELING ACTIVE CONNECTION TASK NOW...", LogType.InfoLog);
            this.AsyncConnectionTokenSource.Cancel(false);
            this.PipeLogger.WriteLog("CANCELED BACKGROUND ACTIVITY OK!", LogType.WarnLog);
            return true;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// This boots up a new background thread which reads our pipes over and over as long as we want
        /// </summary>
        internal bool StartBackgroundReadProcess()
        {
            // Start by building a task object if we need to. If already built, just exit out
            if (this.BackgroundRefreshSource is { IsCancellationRequested: false }) return false;
            if (!this.FulcrumPipe.IsConnected)
            {
                // Log information and throw if auto connection is false
                this.PipeLogger.WriteLog("WARNING! A READ ROUTINE WAS CALLED WITHOUT ENSURING A PIPE CONNECTION WAS BUILT!", LogType.WarnLog);
                this.PipeLogger.WriteLog("CALLING A CONNECTION METHOD BEFORE INVOKING THIS READING ROUTINE TO ENSURE DATA WILL BE READ...", LogType.WarnLog); 
                throw new InvalidOperationException("FAILED TO CONNECT TO OUR PIPE SERVER BEFORE READING OUTPUT FROM SHIM DLL!");
            }

            // Build token, build source and then log information
            this.BackgroundRefreshSource = new CancellationTokenSource();
            this.PipeLogger.WriteLog("BUILT NEW TASK CONTROL OBJECTS FOR READING PROCESS OK!", LogType.InfoLog);

            // Now read forever. Log a warning if no event is hooked onto our reading application event
            if (this.PipeDataProcessed == null) this.PipeLogger.WriteLog("WARNING! READER EVENT IS NOT CONFIGURED! THIS DATA MAY GO TO WASTE!", LogType.WarnLog);
            Task.Run(() =>
            {
                // Log booting process now and run
                this.PipeLogger.WriteLog("PREPARING TO READ INPUT PIPE DATA ON REPEAT NOW...", LogType.InfoLog);
                while (true)
                {
                    // Now read the information needed after a connection has been established ok
                    if (this.ReadPipeData(out string NextPipeData)) continue; 

                    // If failed, then break this loop here
                    this.PipeLogger.WriteLog("FAILED TO READ NEW DATA DUE TO A FATAL ERROR OF SOME TYPE!", LogType.ErrorLog);
                    this.PipeLogger.WriteLog($"EXCEPTION GENERATED FROM READER: {NextPipeData}", LogType.ErrorLog);

                    // Cancel the tasks, break out.
                    this.StopBackgroundReadProcess();
                    this.PipeLogger.WriteLog("STOPPED EXECUTION OF BACKGROUND READING FROM INTERNAL THREAD!", LogType.WarnLog);
                    break;
                }

                // Null out our cancel token and source
                this.PipeLogger.WriteLog("RESET CANCELLATION TOKEN SOURCE OBJECT TO A NULL STATE! READY TO RETRY READING WHEN INVOKED...", LogType.InfoLog);
            }, this.BackgroundRefreshSource.Token);
            return true;
        }
        /// <summary>
        /// Kills our reading process if the process is currently running
        /// </summary>
        /// <returns></returns>
        internal bool StopBackgroundReadProcess()
        {
            // Check if the source or token are null
            if (this.BackgroundRefreshSource == null) {
                this.PipeLogger.WriteLog("TOKENS AND SOURCES WERE NOT YET CONFIGURED WHICH MEANS READING WAS NOT STARTED!", LogType.WarnLog);
                return false;
            }

            // Cancel here and return
            this.PipeLogger.WriteLog("CANCELING ACTIVE READING TASK NOW...", LogType.InfoLog);
            this.BackgroundRefreshSource.Cancel(false);
            this.PipeLogger.WriteLog("CANCELED BACKGROUND ACTIVITY OK!", LogType.WarnLog);
            return true;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Attempts to read data from our pipe server instance.
        /// </summary>
        /// <param name="ReadDataContents">Data processed</param>
        /// <returns>True if content comes back. False if not.</returns>
        internal bool ReadPipeData(out string ReadDataContents)
        {
            // Store our new settings for the pipe buffer and timeout
            DefaultBufferValue = InjectorConstants.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Buffer Size", DefaultBufferValue);
            DefaultReadingTimeout = InjectorConstants.InjectorPipeConfigSettings.GetSettingValue("Reader Pipe Processing Timeout", DefaultReadingTimeout);

            // Build new timeout token values for reading operation and read now
            byte[] OutputBuffer = new byte[DefaultBufferValue];
            List<string> AllProcessedMessages = new List<string>();
            this.AsyncReadingOperationsTokenSource = new CancellationTokenSource(DefaultReadingTimeout);

            try
            {
                // Read input content and check how many bytes we've pulled in
                var ReadingTask = this.FulcrumPipe.ReadAsync(OutputBuffer, 0, OutputBuffer.Length);
                try { ReadingTask.Wait(this.AsyncReadingOperationsTokenSource.Token); }
                catch (Exception AbortEx) { if (AbortEx is not OperationCanceledException) throw AbortEx; }

                // Now convert our bytes into a string object, and print them to our log files.
                int BytesRead = ReadingTask.Result;
                if (BytesRead != OutputBuffer.Length) OutputBuffer = OutputBuffer.Take(BytesRead).ToArray();
                string NextPipeString = Encoding.Default.GetString(OutputBuffer, 0, OutputBuffer.Length);
                string[] SplitPipeContent = NextPipeString.Split('\n')
                    .ToList().Where(StringObj => !string.IsNullOrWhiteSpace(StringObj))
                    .Select(StringPart => StringPart.Trim()).ToArray();

                // Log new message pulled and write contents of it to our log file
                this.PipeLogger.WriteLog($"[PIPE DATA] ::: NEW PIPE DATA PROCESSED!", LogType.TraceLog);
                foreach (var PipeStringPart in SplitPipeContent)
                {
                    // Add this into our list of app pipe content
                    AllProcessedMessages.Add(PipeStringPart);
                    this.PipeLogger.WriteLog($"--> {PipeStringPart}", LogType.TraceLog);

                    // Now fire off a pipe data read event if possible. Otherwise return
                    if (this.PipeDataProcessed == null) continue;
                    this.OnPipeDataProcessed(new FulcrumPipeDataReadEventArgs()
                    {
                        // Store byte values
                        PipeByteData = OutputBuffer,
                        ByteDataLength = (uint)OutputBuffer.Length,

                        // Store string values
                        PipeDataString = PipeStringPart,
                        PipeDataStringLength = (uint)PipeStringPart.Length
                    });
                }

                // Return passed and build output string values
                ReadDataContents = string.Join("\n", AllProcessedMessages).Trim();
                return true;
            }
            catch (Exception ReadEx)
            {
                // Log our failures and return failed output
                this.PipeLogger.WriteLog("FAILED TO READ NEW PIPE INPUT DATA!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING READING OPERATIONS OF OUR INPUT PIPE DATA PROCESSING!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ReadEx);

                // Return failed
                ReadDataContents = $"FAILED_PIPE_READ__{ReadEx.GetType().Name.ToUpper()}";
                return false;
            }
        }
    }
}