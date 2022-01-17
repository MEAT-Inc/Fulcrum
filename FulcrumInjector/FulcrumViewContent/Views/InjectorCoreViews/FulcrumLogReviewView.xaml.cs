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
using FulcrumInjector.FulcrumLogic.InjectorPipes;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport.AppStyleSupport.AvalonEditHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumLogReviewView.xaml
    /// </summary>
    public partial class FulcrumLogReviewView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorLogReviewViewLogger")) ?? new SubServiceLogger("InjectorLogReviewViewLogger");

        // ViewModel object to bind onto
        public FulcrumLogReviewViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for DLL Logging output view
        /// </summary>
        public FulcrumLogReviewView()
        {
            // Build new ViewModel object
            InitializeComponent();
            this.ViewModel = InjectorConstants.FulcrumLogReviewViewModel ?? new FulcrumLogReviewViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumLogReviewView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Log completed setup values ok
            this.ViewLogger.WriteLog("SETUP A NEW PIPE READING EVENT OBJECT TO PROCESS INPUT LOG FILES FOR REVIEW OK!", LogType.WarnLog);
        }

        /// <summary>
        /// Action processing for when clicking the load file button for the injector.
        /// </summary>
        /// <param name="SendingButton"></param>
        /// <param name="SenderEventArgs"></param>
        private void LoadInjectorLogFile_OnClick(object SendingButton, RoutedEventArgs SenderEventArgs)
        {
            // TODO: Build file explorer popup for select a new file instance then load it using the VM. 
            // TODO: Show a flyout object here for building in new content to indicate loading.
        }
    }
}
