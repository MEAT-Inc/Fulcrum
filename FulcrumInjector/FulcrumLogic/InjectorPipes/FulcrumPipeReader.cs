using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        public static void ResetPipeInstance()
        {
            // Reset Pipe here if needed and can
            if (PipeInstance == null) { return; }
            if (PipeInstance.PipeState != FulcrumPipeState.Connected) PipeInstance.AttemptPipeConnection();
            else { PipeInstance.PipeLogger.WriteLog("READER PIPE WAS ALREADY CONNECTED! NOT RECONFIGURING IT!", LogType.WarnLog); }
        }

        // -------------------------------------------------------------------------------------------------------

        // Default value for pipe processing buffer
        private static int DefaultBufferValue = 10240;
        private static int DefaultReadTimeout = 5000;
        public static bool IsConnecting { get; private set; }

        // Task objects for monitoring background readers
        private CancellationToken BackgroundRefreshToken;
        private CancellationTokenSource BackgroundRefreshSource;

        // Pipe and reader objects for data
        internal NamedPipeClientStream FulcrumPipe;

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
                ".",                             // Name of the pipe host
                base.FulcrumPipeAlpha,                   // Name of the pipe client
                PipeDirection.In,                        // Pipe directional configuration
                PipeOptions.Asynchronous,                // Pipe operational modes
                TokenImpersonationLevel.Impersonation    // Token spoofing mode is set to none.
            );

            // Build our new pipe instance here.
            if (this.AttemptPipeConnection()) return;

            // Log failed to open and return if failed
            this.PipeLogger.WriteLog("FAILED TO CONFIGURE NEW INPUT READER PIPE!", LogType.ErrorLog);
            this.PipeLogger.WriteLog("PIPE FAILURE WILL BE MONITORED AND CONNECTIONS WILL BE RETRIED ON A PRESET INTERVAL...", LogType.WarnLog);
        }

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new pipe instance for our type provided.
        /// </summary>
        /// <returns>True if the pipe was built OK. False if not.</returns>
        internal override bool AttemptPipeConnection()
        {
            // Find our read timeout setting value now then apply it based on how our settings objects look
            var ReadTimeoutSetting = InjectorConstants.InjectorPipeConfigSettings.SettingsEntries
                .FirstOrDefault(SettingObj => SettingObj.SettingName == "Reader Pipe Timeout");
            if (ReadTimeoutSetting == null)
                this.PipeLogger.WriteLog($"WARNING: SETTING FOR READ TIMEOUT WAS NULL! DEFAULTING TO {DefaultReadTimeout}", LogType.TraceLog);
            DefaultReadTimeout = ReadTimeoutSetting?.SettingValue as int? ?? DefaultReadTimeout;

            try
            {
                // Log ready for connection and send it.
                this.PipeState = FulcrumPipeState.Open;
                this.StartAsyncHostConnect();
                this.PipeLogger.WriteLog("PIPE CLIENT STREAM HAS BEEN CONFIGURED! ATTEMPTING CONNECTION ON IT NOW...", LogType.WarnLog);
                this.PipeLogger.WriteLog($"WAITING FOR {DefaultReadTimeout} MILLISECONDS BEFORE THE PIPES WILL TIMEOUT DURING THE CONNECTION ROUTINE", LogType.TraceLog);
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

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Async connects to our client on the reader side of operations
        /// </summary>
        private void StartAsyncHostConnect()
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
                return;
            }

            // Set connection building bool value and update view if possible
            IsConnecting = true;
            if (InjectorConstants.InjectorMainWindow != null) {
                InjectorConstants.InjectorMainWindow.Dispatcher.Invoke(() =>
                {
                    InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = false;
                    InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Working...";
                });
            }

            // Run our connection routine
            this.PipeLogger.WriteLog("STARTING READER PIPE CONNECTION ROUTINE NOW...", LogType.WarnLog);
            Task.Run(() =>
            {
                // Build pipe reading stream object
                try { this.FulcrumPipe.Connect(DefaultReadTimeout); }
                catch (Exception ConnectEx)
                {
                    // Connecting to false
                    IsConnecting = false;
                    if (ConnectEx is not TimeoutException)
                    {
                        // Throw our exception if it's not timeout based
                        this.PipeState = FulcrumPipeState.Faulted;
                        if (InjectorConstants.InjectorMainWindow == null) throw ConnectEx;

                        // Reset button state and content
                        InjectorConstants.InjectorMainWindow.Dispatcher.Invoke(() =>
                        {
                            InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Test Injection";
                            InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = true;
                        });

                        // Throw exception and return
                        throw ConnectEx;
                    }

                    // Set state to disconnected. Log failure
                    this.PipeState = FulcrumPipeState.Disconnected;
                    this.PipeLogger.WriteLog("FAILED TO CONNECT TO HOST PIPE SERVER AFTER GIVEN TIMEOUT VALUE!", LogType.WarnLog);

                    // Reset button state and content
                    if (InjectorConstants.InjectorMainWindow == null) { IsConnecting = false; return; }
                    InjectorConstants.InjectorMainWindow.Dispatcher.Invoke(() =>
                    {
                        InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Test Injection";
                        InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = true;
                        IsConnecting = false;
                    });

                    // Exit method
                    return;
                }

                // If we're connected, log that information and break out
                this.PipeState = FulcrumPipeState.Connected;
                this.PipeLogger.WriteLog("CONNECTED NEW SERVER INSTANCE TO OUR READER!", LogType.WarnLog);
                this.PipeLogger.WriteLog($"PIPE SERVER CONNECTED TO FULCRUM PIPER {this.PipeType} OK!", LogType.InfoLog);

                // Reset button state and content
                if (InjectorConstants.InjectorMainWindow == null) { IsConnecting = false; return; }
                InjectorConstants.InjectorMainWindow.Dispatcher.Invoke(() =>
                {
                    InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Test Injection";
                    InjectorConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = true;
                });

                // Set connecting to false 
                IsConnecting = false;
            });
        }

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// This boots up a new background thread which reads our pipes over and over as long as we want
        /// </summary>
        internal bool StartBackgroundReadProcess()
        {
            // Start by building a task object if we need to. If already built, just exit out
            if (this.BackgroundRefreshSource is { IsCancellationRequested: false }) return false;

            // Build token, build source and then log information
            this.BackgroundRefreshSource = new CancellationTokenSource();
            this.BackgroundRefreshToken = this.BackgroundRefreshSource.Token;
            this.PipeLogger.WriteLog("BUILT NEW TASK CONTROL OBJECTS FOR READING PROCESS OK!", LogType.InfoLog);

            // Now read forever. Log a warning if no event is hooked onto our reading application event
            if (this.PipeDataProcessed == null) this.PipeLogger.WriteLog("WARNING! READER EVENT IS NOT CONFIGURED! THIS DATA MAY GO TO WASTE!", LogType.WarnLog);
            Task.Run(() =>
            {
                // Log booting process now and run
                this.PipeLogger.WriteLog("PREPARING TO READ INPUT PIPE DATA ON REPEAT NOW...", LogType.InfoLog);
                while (!BackgroundRefreshToken.IsCancellationRequested)
                {
                    // Find if the read attempt truly fails or not
                    bool PipeResult = this.ReadPipeData(out string NextPipeData);
                    if (PipeResult) continue;

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
            }, BackgroundRefreshToken);
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
            this.BackgroundRefreshSource.Cancel();
            this.PipeLogger.WriteLog("CANCELED BACKGROUND ACTIVITY OK!", LogType.WarnLog);

            // Build new source and token
            this.BackgroundRefreshSource = new CancellationTokenSource();
            this.BackgroundRefreshToken = this.BackgroundRefreshSource.Token;
            return true;
        }

        /// <summary>
        /// Attempts to read data from our pipe server instance.
        /// </summary>
        /// <param name="ReadDataContents">Data processed</param>
        /// <returns>True if content comes back. False if not.</returns>
        internal bool ReadPipeData(out string ReadDataContents)
        {
            // Find our buffer size value here first. Pull it from our settings pane
            var BufferSizeSetting = InjectorConstants.InjectorPipeConfigSettings.SettingsEntries
                .FirstOrDefault(SettingObj => SettingObj.SettingName == "Pipe Reader Buffer Size");
            if (BufferSizeSetting == null) 
                this.PipeLogger.WriteLog($"WARNING: SETTING FOR BUFFER WAS NULL! DEFAULTING TO {DefaultBufferValue}", LogType.TraceLog);

            try
            {
                // Now build said buffer value using the pulled value or using a predefined constant
                int BytesRead = 0;
                do
                {
                    // Build our output buffer of bytes
                    byte[] OutputBuffer = new byte[BufferSizeSetting?.SettingValue as int? ?? DefaultBufferValue];

                    // Read input content and check how many bytes we've pulled in
                    BytesRead = this.FulcrumPipe.Read(OutputBuffer, 0, OutputBuffer.Length);
                    if (BytesRead == 0) { ReadDataContents = "EMPTY_OUTPUT"; return true; }
                    this.PipeLogger.WriteLog($"--> PIPE READER PROCESSED {BytesRead} BYTES OF INPUT", LogType.TraceLog);

                    // Trim off the excess byte values here if needed
                    if (BytesRead != OutputBuffer.Length) {
                        OutputBuffer = OutputBuffer.Take(BytesRead).ToArray();
                        this.PipeLogger.WriteLog($"--> TRIMMED A TOTAL OF {OutputBuffer.Length - BytesRead} BYTES OFF OUR READ INPUT", LogType.TraceLog);
                    }

                    // Now convert our bytes into a string object, and print them to our log files.
                    ReadDataContents = Encoding.Default.GetString(OutputBuffer, 0, OutputBuffer.Length);
                    this.PipeLogger.WriteLog($"--> PIPE STRING DATA PROCESSED: {ReadDataContents}", LogType.TraceLog);

                    // Now fire off a pipe data read event if possible. Otherwise return
                    if (this.PipeDataProcessed == null) {
                        this.PipeLogger.WriteLog("WARNING! READER EVENT IS NOT CONFIGURED! THIS DATA MAY GO TO WASTE!", LogType.WarnLog);
                        return true;
                    }

                    // Invoke the event here and send event out
                    this.PipeLogger.WriteLog("INVOKING NEW EVENT FOR PIPE DATA PROCESSED NOW...", LogType.TraceLog);
                    this.OnPipeDataProcessed(new FulcrumPipeDataReadEventArgs()
                    {
                        // Store byte values
                        PipeByteData = OutputBuffer,
                        ByteDataLength = (uint)OutputBuffer.Length,

                        // Store string values
                        PipeDataString = ReadDataContents,
                        PipeDataStringLength = (uint)ReadDataContents.Length
                    });
                } while (BytesRead > 0 && !this.FulcrumPipe.IsMessageComplete);

                // Return passed output
                return true;
            }
            catch (Exception ReadEx)
            {
                // Log our failures and return failed output
                this.PipeLogger.WriteLog("FAILED TO READ NEW PIPE INPUT DATA!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN DURING READING OPERATIONS OF OUR INPUT PIPE DATA PROCESSING!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("EXCEPTION THROWN IS LOGGED BELOW", ReadEx);

                // Return failed
                ReadDataContents = $"FAILED_READ__{ReadEx.GetType().Name.ToUpper()}";
                return false;
            }
        }
    }
}