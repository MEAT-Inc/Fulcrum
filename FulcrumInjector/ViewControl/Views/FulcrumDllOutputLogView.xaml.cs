using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.AppLogic.AvalonEditHelpers;
using FulcrumInjector.ViewControl.ViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Views
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
            InitializeComponent();
            this.ViewModel = new FulcrumDllOutputLogViewModel();
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
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches for the provided text values
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DllLogFilteringTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the current text entry value and pass it over to the VM for actions.
            var FilteringTextBox = (TextBox)sender;
            string TextToFilter = FilteringTextBox.Text;

            // Run the search and show method on the view model
            await Task.Run(() =>
            {
                // Disable textbox for the duration of the task
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = false);
                ViewModel.SearchForText(TextToFilter);
                Dispatcher.Invoke(() => FilteringTextBox.IsEnabled = true);
            });
        }
    }
}
