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
using FulcrumInjector.ViewControl.ViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Views
{
    /// <summary>
    /// Interaction logic for FulcrumPipeStatusView.xaml
    /// </summary>
    public partial class FulcrumPipeStatusView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("PipeStatusViewLogger")) ?? new SubServiceLogger("PipeStatusViewLogger");

        // ViewModel object to bind onto
        public FulcrumPipeStatusViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumPipeStatusView()
        {
            // Init component. Build new VM object
            InitializeComponent();
            this.ViewModel = new FulcrumPipeStatusViewModel();
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumPipeStatusView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Configure pipe instances here.
            this.ViewModel.SetupPipeModelStates();
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND WATCHDOGS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
