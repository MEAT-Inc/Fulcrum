using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumLogic.InjectorPipes;
using FulcrumInjector.FulcrumLogic.InjectorPipes.PipeEvents;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport.AppStyleSupport.AvalonEditHelpers;
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
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorDllOutputViewLogger")) ?? new SubServiceLogger("InjectorDllOutputViewLogger");

        // ViewModel object to bind onto
        public FulcrumDllOutputLogViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for DLL Logging output view
        /// </summary>
        public FulcrumDllOutputLogView()
        {
            // Init component. Build new VM object
            InitializeComponent();

            // Find the global color sheet and store values for it.
            this.Resources.MergedDictionaries.Add(Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(Dict => Dict.Source.ToString().Contains("AppColorTheme")));
            ViewLogger.WriteLog($"SETUP MAIN COLOR THEME FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);

            // Store view model instance.
            this.ViewModel = InjectorConstants.FulcrumDllOutputLogViewModel ?? new FulcrumDllOutputLogViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
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

            // Configure pipe instances here.
            this.ViewModel.LogContentHelper = new AvalonEditFilteringHelpers(this.DebugRedirectOutputEdit);
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR FULCRUM DLL OUTPUT OK!", LogType.InfoLog);

            // Build event for our pipe objects to process new pipe content into our output box
            FulcrumPipeReader.PipeInstance.PipeDataProcessed += (_, EventArgs) => {
                Dispatcher.Invoke(() => this.DebugRedirectOutputEdit.Text += EventArgs.PipeDataString + "\n");
            };

            // Log completed setup values ok
            this.ViewLogger.WriteLog("SETUP A NEW PIPE READING EVENT OBJECT TO PROCESS OUR OUTPUT PIPE CONTENTS INTO THE DLL OUTPUT BOX OK!", LogType.WarnLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches for the provided text values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DllLogFilteringTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the current text entry value and pass it over to the VM for actions.
            var FilteringTextBox = (TextBox)sender;
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
    }
}
