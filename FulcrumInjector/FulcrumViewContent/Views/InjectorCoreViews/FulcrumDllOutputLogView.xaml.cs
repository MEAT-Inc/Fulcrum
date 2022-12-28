using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FulcrumInjector.FulcrumLogic.FulcrumPipes;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.DebugLogFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.FIlteringFormatters;
using FulcrumInjector.FulcrumViewSupport.AvalonEditHelpers.InjectorSyntaxFormatters;
using ICSharpCode.AvalonEdit;
using NLog;
using NLog.Config;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for InjectorDllOutputLogView.xaml
    /// </summary>
    public partial class FulcrumDllOutputLogView : UserControl
    {  
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("InjectorDllOutputViewLogger", LoggerActions.SubServiceLogger);

        // ViewModel object to bind onto
        public FulcrumDllOutputLogViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for DLL Logging output view
        /// </summary>
        public FulcrumDllOutputLogView()
        {
            // Initialize new UI Component
            InitializeComponent();
            this.ViewModel = FulcrumConstants.FulcrumDllOutputLogViewModel ?? new FulcrumDllOutputLogViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);

            // Build event for our pipe objects to process new pipe content into our output box
            FulcrumPipeReader.PipeInstance.PipeDataProcessed += this.ViewModel.OnPipeReaderContentProcessed;
            this.ViewLogger.WriteLog("STORED NEW EVENT BROKER FOR PIPE READING DATA PROCESSED OK!", LogType.InfoLog);

            // Configure the new Logging Output Target.
            var CurrentConfig = LogManager.Configuration;
            if (CurrentConfig.AllTargets.All(TargetObj => TargetObj.Name != "LiveInjectorOutputTarget")) {
                this.ViewLogger.WriteLog("WARNING: TARGET WAS ALREADY CONFIGURED AND FOUND! NOT BUILDING AGAIN", LogType.WarnLog);
                return;
            }

            // Log information, build new target output and return.
            ViewLogger.WriteLog("NO TARGETS MATCHING DEFINED TYPE WERE FOUND! THIS IS A GOOD THING", LogType.InfoLog);
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("LiveInjectorOutputTarget", typeof(InjectorOutputSyntaxHelper));
            CurrentConfig.AddRuleForAllLevels(new DebugLoggingRedirectTarget(this.DebugRedirectOutputEdit));

            // Log information about events built.
            this.ViewLogger.WriteLog("BUILT EVENT PROCESSING OBJECTS FOR PIPE OUTPUT AND FOR INJECTOR DLL OUTPUT OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumDLLOutputLogView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Configure filtering and coloring instances here.
            this.ViewModel.LogFilteringHelper ??= new LogOutputFilteringHelper(this.DebugRedirectOutputEdit);
            this.ViewModel.InjectorSyntaxHelper ??= new InjectorOutputSyntaxHelper(this.DebugRedirectOutputEdit);
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR FULCRUM DLL OUTPUT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches for the provided text values
        /// </summary>
        /// <param name="SendingTextBox"></param>
        /// <param name="TextChangedArgs"></param>
        private void LogFilteringTextBox_OnTextChanged(object SendingTextBox, TextChangedEventArgs TextChangedArgs)
        {
            // Get the current text entry value and pass it over to the VM for actions.
            var FilteringTextBox = (TextBox)SendingTextBox;
            string TextToFilter = FilteringTextBox.Text;

            // Run the search and show method on the view model
            Task.Run(() =>
            {
                // Disable TextBox for the duration of the task
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = false);
                ViewModel.SearchForText(TextToFilter);
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = true);
            });
        }

        /// <summary>
        /// Updates has content value on the view model when text is changed.
        /// </summary>
        /// <param name="SendingTextBox"></param>
        /// <param name="TextChangedArgs"></param>
        private void DebugRedirectOutputEdit_OnTextChanged(object SendingTextBox, EventArgs TextChangedArgs)
        {
            // Check the content value. If empty, set hasContent to false.
            TextEditor DebugEditor = (TextEditor)SendingTextBox;
            this.ViewModel.HasOutput = DebugEditor.Text.Trim().Length != 0;
        }

        /// <summary>
        /// Toggles format output for syntax outlining when writing new entries into our log files.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private async void SyntaxHighlightingButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Build new button object.
            Button SendButton = (Button)Sender;
            SendButton.Content = "Toggling...";
            SendButton.Background = Brushes.DarkOrange;

            // Async toggle button content and output format.
            await Task.Run(() =>
            {
                // Check the current state and toggle it.
                if (this.ViewModel.InjectorSyntaxHelper.IsHighlighting)
                    this.ViewModel.InjectorSyntaxHelper.StopColorHighlighting();
                else this.ViewModel.InjectorSyntaxHelper.StartColorHighlighting();
            });

            // Now apply new values to our button.
            SendButton.Background = this.ViewModel.InjectorSyntaxHelper.IsHighlighting ? Brushes.DarkGreen : Brushes.DarkRed;
            SendButton.Content = this.ViewModel.InjectorSyntaxHelper.IsHighlighting ? "Syntax Highlighting: ON" : "Syntax Highlighting: OFF";

            // Log toggle result.
            this.ViewLogger.WriteLog($"TOGGLED HIGHLIGHTING STATE OK! NEW STATE IS {this.ViewModel.InjectorSyntaxHelper.IsHighlighting}", LogType.InfoLog);
        }
    }
}
