﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using FulcrumInjector.FulcrumLogic.InjectorPipes.PipeEvents;
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
            if (PipeInstance.PipeState != FulcrumPipeState.Connected) PipeInstance.ConfigureNewPipe();
            else { PipeInstance.PipeLogger.WriteLog("READER PIPE WAS ALREADY CONNECTED! NOT RECONFIGURING IT!", LogType.WarnLog); }
        }

        // -------------------------------------------------------------------------------------------------------

        // Pipe and reader objects for data
        internal StreamReader PipeReader;
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
                ".",                   // Name of the pipe host
                base.FulcrumPipeAlpha,          // Name of the pipe client
                PipeDirection.In,               // Pipe directional configuration
                PipeOptions.Asynchronous,       // Pipe operational modes
                TokenImpersonationLevel.None    // Token spoofing mode is set to none.
            );

            // Build our new pipe instance here.
            if (!this.ConfigureNewPipe())
            {
                // Log failed to open and return
                this.PipeLogger.WriteLog("FAILED TO CONFIGURE NEW OUTPUT WRITER PIPE!", LogType.ErrorLog);
                this.PipeLogger.WriteLog("PIPE FAILURE WILL BE MONITORED AND CONNECTIONS WILL BE RETRIED ON A PRESET INTERVAL...", LogType.WarnLog);
                return;
            }
        }

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new pipe instance for our type provided.
        /// </summary>
        /// <returns>True if the pipe was built OK. False if not.</returns>
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

        /// <summary>
        /// Attempts to read data from our pipe server instance.
        /// </summary>
        /// <param name="ReadDataContents">Data processed</param>
        /// <returns>True if content comes back. False if not.</returns>
        internal bool ReadPipeData(out string ReadDataContents)
        {
            // Start by making sure we're open
            if (this.PipeState != FulcrumPipeState.Connected) 
                if (!this.ConfigureNewPipe()) { ReadDataContents = "FAILED TO CONFIGURE READER PIPE!"; return false; }

            // Now read in some data from the pipe.
            ReadDataContents = "EMPTY_OUTPUT";
            return true;
        }
    }
}