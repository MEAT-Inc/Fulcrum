using System.Windows.Controls;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using SharpLogging;
using SharpPipes;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// View Model for pipe status values
    /// </summary>
    public class FulcrumPipeStatusViewModel : FulcrumViewModelBase
    {
        #region Custom Events

        // Private property watchdogs to update our UI based on other events 
        // TODO: FUCKING REMOVE THIS? WHY NOT USE PROP CHANGED EVENTS?
        private readonly FulcrumPropertyWatchdog _readerPipeStateWatchdog;
        private readonly FulcrumPropertyWatchdog _writerPipeStateWatchdog;
        private readonly FulcrumPropertyWatchdog _testInjectionButtonWatchdog;
        
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our public properties
        private string _readerPipeState;
        private string _writerPipeState;

        // Private pipe objects to be allocated
        private readonly PassThruPipe _readerPipe;
        private readonly PassThruPipe _writerPipe;

        #endregion //Fields

        #region Properties

        // Public properties for the view to bind onto  
        public string ReaderPipeState { get => _readerPipeState; set => PropertyUpdated(value); }
        public string WriterPipeState { get => _writerPipeState; set => PropertyUpdated(value); }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="PipeStateUserControl">UserControl which holds the content for the pipe status view</param>
        public FulcrumPipeStatusViewModel(UserControl PipeStateUserControl) : base(PipeStateUserControl)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger); 
            this.ViewModelLogger.WriteLog("SETTING UP PIPE STATUS VIEW BOUND VALUES NOW...", LogType.WarnLog);
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);

            // Configure new pipe instances for our class
            this._readerPipe = PassThruPipeReader.AllocatePipe();
            this._writerPipe = PassThruPipeWriter.AllocatePipe();

            // Build new pipe model object and watchdogs.
            this._readerPipeStateWatchdog = new FulcrumPropertyWatchdog(250);
            this._writerPipeStateWatchdog = new FulcrumPropertyWatchdog(250);
            this._testInjectionButtonWatchdog = new FulcrumPropertyWatchdog(250);
            this.ViewModelLogger.WriteLog("BUILT NEW MODEL OBJECT AND WATCHDOG OBJECTS FOR PIPE INSTANCES OK!");
            this.ViewModelLogger.WriteLog("SETUP NEW PIPE STATUS MONITOR VALUES OK!");
            this.ViewModelLogger.WriteLog($"VIEW MODEL TYPE {this.GetType().Name} HAS BEEN CONSTRUCTED CORRECTLY!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new watchdogs for our pipe state monitoring output
        /// </summary>
        public void SetupPipeStateWatchdogs()
        {
            // Reader State and Writer State watchdogs for the current pipe state values.
            // BUG: Trying to understand why these two watchdogs are hanging up the UI. Seems logging operations hang forever? Maybe turn down frequency
            this._readerPipeStateWatchdog.StartUpdateTimer((_, _) => { this.ReaderPipeState = this._readerPipe.PipeState.ToString(); });
            this._writerPipeStateWatchdog.StartUpdateTimer((_, _) => { this.WriterPipeState = this._writerPipe.PipeState.ToString(); });

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
                switch (PassThruPipeReader.IsConnecting)
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

            // Start the allocate routines now if needed
            this._readerPipe.StartPipeConnectionAsync();
            this._writerPipe.StartPipeConnectionAsync();

            // Log built and output information.
            this.ViewModelLogger.WriteLog("CONFIGURED AND STARTED NEW WATCHDOGS FOR THE READER AND WRITER PIPE STATE VALUES!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"READER IS UPDATING AT A RATE OF {this._readerPipeStateWatchdog.UpdateInterval.Milliseconds}");
            this.ViewModelLogger.WriteLog($"WRITER IS UPDATING AT A RATE OF {this._writerPipeStateWatchdog.UpdateInterval.Milliseconds}");
            this.ViewModelLogger.WriteLog($"TESTER IS UPDATING AT A RATE OF {this._testInjectionButtonWatchdog.UpdateInterval.Milliseconds}");
        }
    }
}
