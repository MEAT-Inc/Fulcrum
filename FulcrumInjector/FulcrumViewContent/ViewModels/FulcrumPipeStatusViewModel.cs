using System;
using System.Linq;
using System.Threading;
using FulcrumInjector.FulcrumLogic.FulcrumPipes;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.DataContentHelpers;
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
        private PropertyWatchdog _testInjectionButtonWatchdog;
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
            this._readerPipeStateWatchdog = new PropertyWatchdog(250);
            this._writerPipeStateWatchdog = new PropertyWatchdog(250);
            this._testInjectionButtonWatchdog = new PropertyWatchdog(250);
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
            // Reader State and Writer State watchdogs for the current pipe state values.
            // BUG: Trying to understand why these two watchdogs are hanging up the UI. Seems logging operations hang forever? Maybe turn down frequency
            this._readerPipeStateWatchdog.StartUpdateTimer((_, _) => { this.ReaderPipeState = FulcrumPipeReader.PipeInstance.PipeState.ToString(); });
            this._writerPipeStateWatchdog.StartUpdateTimer((_, _) => { this.WriterPipeState = FulcrumPipeWriter.PipeInstance.PipeState.ToString(); });

            // Injector button state watchdog
            this._testInjectionButtonWatchdog.StartUpdateTimer((_,_) =>
            {
                // For app setup and loading values
                if (this.WriterPipeState == "Connected" && this.ReaderPipeState == "Connected")
                    FulcrumConstants.FulcrumDllInjectionTestViewModel.InjectionLoadPassed = true;

                // Check Values of pipe states and build UI content accordingly
                if (FulcrumConstants.FulcrumDllInjectionTestViewModel.InjectionLoadPassed) {
                    FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = false;
                    FulcrumConstants.FulcrumDllInjectionTestViewModel.InjectorTestResult = "Injection Passed!";
                    FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Test Injection";

                    // Stop updating values here once we get a good injection test to run.
                    this._testInjectionButtonWatchdog.PropertyUpdateTimer.Stop();
                    return; 
                }
                
                // For app setup and loading values
                if (this.WriterPipeState == "Loading..." || this.ReaderPipeState == "Loading...") {
                    FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = false;                    
                    return; 
                }

                // Set content based on injector state values
                switch (FulcrumPipeReader.IsConnecting)
                {
                    // If injector is connecting
                    case true:
                        FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = false;
                        FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Working...";
                        break;

                    // If not connected and not run yet
                    case false when FulcrumConstants.FulcrumDllInjectionTestViewModel.InjectionLoadPassed == false:
                        FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = true;
                        FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.Content = "Test Injection";
                        break;
                }
            });

            // Log built and output information.
            ViewModelLogger.WriteLog("CONFIGURED AND STARTED NEW WATCHDOGS FOR THE READER AND WRITER PIPE STATE VALUES!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"READER IS UPDATING AT A RATE OF {this._readerPipeStateWatchdog.UpdateInterval.Milliseconds}");
            ViewModelLogger.WriteLog($"WRITER IS UPDATING AT A RATE OF {this._writerPipeStateWatchdog.UpdateInterval.Milliseconds}");
            ViewModelLogger.WriteLog($"TESTER IS UPDATING AT A RATE OF {this._testInjectionButtonWatchdog.UpdateInterval.Milliseconds}");
        }
    }
}
