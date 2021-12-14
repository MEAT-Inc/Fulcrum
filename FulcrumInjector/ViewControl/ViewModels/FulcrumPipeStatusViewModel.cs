using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic.InjectorPipes;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl.Models;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.ViewModels
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
        private readonly FulcrumPipeStatusModel _pipeStatusModel;

        // Public values for our view to bind onto 
        public string ReaderPipeState { get => _readerPipeState; set => PropertyUpdated(value); }
        public string WriterPipeState { get => _writerPipeState; set => PropertyUpdated(value); }
        public FulcrumPipeStatusModel PipeStatusModel { get => _pipeStatusModel; set => PropertyUpdated(value); }

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
            this._pipeStatusModel = new FulcrumPipeStatusModel();
            this._readerPipeStateWatchdog = new PropertyWatchdog();
            this._writerPipeStateWatchdog = new PropertyWatchdog();
            ViewModelLogger.WriteLog("BUILT NEW MODEL OBJECT AND WATCHDOG OBJECTS FOR PIPE INSTANCES OK!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW PIPE STATUS MONITOR VALUES OK!", LogType.InfoLog);
        }


        /// <summary>
        /// Builds our new fulcrum state watchdog values here.
        /// </summary>
        public void SetupPipeModelStates()
        {
            // Once model objects are built, we can then validate our pipes.
            this.PipeStatusModel.ValidateFulcrumPipeConfiguration();
            ViewModelLogger.WriteLog("PIPE VALIDATION COMPLETE! READY TO SHOW OUR PROCESS INFORMATION ON THIS VIEW COMPONENT");
            ViewModelLogger.WriteLog("CONFIGURING NEW WATCHDOG INSTANCES FOR OUR PIPE STATE PROPERTY MONITORS NOW...", LogType.InfoLog);

            // Reader State and Writer State watchdogs
            this._readerPipeStateWatchdog.StartUpdateTimer((_, _) =>
            {
                // Check current value.
                if (this.ReaderPipeState != this.PipeStatusModel.AlphaPipe.PipeState.ToString())
                    ViewModelLogger.WriteLog($"[WATCHDOG UPDATE] ::: READER PIPE STATE HAS BEEN MODIFIED! NEW VALUE {this.ReaderPipeState}", LogType.TraceLog);

                // Store value
                this.ReaderPipeState = this.PipeStatusModel.AlphaPipe.PipeState.ToString();
            });
            this._writerPipeStateWatchdog.StartUpdateTimer((_, _) =>
            {
                // Check current value.
                if (this.WriterPipeState != this.PipeStatusModel.BravoPipe.PipeState.ToString()) 
                    ViewModelLogger.WriteLog($"[WATCHDOG UPDATE] ::: WRITER PIPE STATE HAS BEEN MODIFIED! NEW VALUE {this.WriterPipeState}", LogType.TraceLog);
                
                // Store value
                this.WriterPipeState = this.PipeStatusModel.BravoPipe.PipeState.ToString();
            });

            // Log built and output information.
            ViewModelLogger.WriteLog("CONFIGURED AND STARTED NEW WATCHDOGS FOR THE READER AND WRITER PIPE STATE VALUES!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"READER IS UPDATING AT A RATE OF {this._readerPipeStateWatchdog.UpdateInterval.Milliseconds}");
            ViewModelLogger.WriteLog($"WRITER IS UPDATING AT A RATE OF {this._writerPipeStateWatchdog.UpdateInterval.Milliseconds}");
        }
    }
}
