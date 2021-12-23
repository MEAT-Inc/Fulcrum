﻿using System.Linq;
using System.Threading;
using FulcrumInjector.FulcrumLogic.InjectorPipes;
using FulcrumInjector.FulcrumViewContent.Models;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// View Model for pipe status values
    /// </summary>
    public class FulcrumPipeStatusViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("PipeStatusViewModelLogger")) ?? new SubServiceLogger("PipeStatusViewModelLogger");

        // Private Control Values
        private string _readerPipeState;
        private string _writerPipeState;
        private PropertyWatchdog _readerPipeStateWatchdog;
        private PropertyWatchdog _writerPipeStateWatchdog;

        // Public values for our view to bind onto 
        public string ReaderPipeState { get => _readerPipeState; set => PropertyUpdated(value); }
        public string WriterPipeState { get => _writerPipeState; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumPipeStatusViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP PIPE STATUS VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Build new pipe model object and watchdogs.
            this._readerPipeStateWatchdog = new PropertyWatchdog();
            this._writerPipeStateWatchdog = new PropertyWatchdog();
            ViewModelLogger.WriteLog("BUILT NEW MODEL OBJECT AND WATCHDOG OBJECTS FOR PIPE INSTANCES OK!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW PIPE STATUS MONITOR VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new watchdogs for our pipe state monitoring output
        /// </summary>
        internal void SetupPipeStateWatchdogs()
        {
            // Reader State and Writer State watchdogs
            this._readerPipeStateWatchdog.StartUpdateTimer((_, _) =>
            {
                // Check current value. If unchanged, drop out
                if (this.ReaderPipeState == FulcrumPipeReader.PipeInstance.PipeState.ToString()) {
                    this.ReaderPipeState = FulcrumPipeReader.PipeInstance.PipeState.ToString();
                    return;
                }

                // Log new value output and store it
                this.ReaderPipeState = FulcrumPipeReader.PipeInstance.PipeState.ToString();
                ViewModelLogger.WriteLog($"[WATCHDOG UPDATE] ::: READER PIPE STATE HAS BEEN MODIFIED! NEW VALUE {this.ReaderPipeState}", LogType.TraceLog);
            });
            this._writerPipeStateWatchdog.StartUpdateTimer((_, _) =>
            {
                // Check current value. If unchanged, drop out
                if (this.WriterPipeState == FulcrumPipeWriter.PipeInstance.PipeState.ToString()) {
                    this.WriterPipeState = FulcrumPipeWriter.PipeInstance.PipeState.ToString();
                    return;
                }

                // Log new value output and store it.
                this.WriterPipeState = FulcrumPipeWriter.PipeInstance.PipeState.ToString();
                ViewModelLogger.WriteLog($"[WATCHDOG UPDATE] ::: WRITER PIPE STATE HAS BEEN MODIFIED! NEW VALUE {this.WriterPipeState}", LogType.TraceLog);
            });

            // Log built and output information.
            ViewModelLogger.WriteLog("CONFIGURED AND STARTED NEW WATCHDOGS FOR THE READER AND WRITER PIPE STATE VALUES!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"READER IS UPDATING AT A RATE OF {this._readerPipeStateWatchdog.UpdateInterval.Milliseconds}");
            ViewModelLogger.WriteLog($"WRITER IS UPDATING AT A RATE OF {this._writerPipeStateWatchdog.UpdateInterval.Milliseconds}");
        }
    }
}
