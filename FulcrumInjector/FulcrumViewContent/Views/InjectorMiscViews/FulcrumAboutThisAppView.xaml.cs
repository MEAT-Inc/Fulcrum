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
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorMiscViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for AboutThisAppView.xaml
    /// </summary>
    public partial class FulcrumAboutThisAppView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("AboutThisAppViewLogger", LoggerActions.SubServiceLogger);

        // ViewModel object to bind onto
        public FulcrumAboutThisAppViewModel ViewModel { get; set; }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumAboutThisAppView()
        {
            InitializeComponent();
            this.ViewModel = new FulcrumAboutThisAppViewModel();
            this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumAboutThisAppView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            ViewModel.SetupViewControl(this);
            DataContext = ViewModel;

            // Log booted title view
            this.ViewLogger.WriteLog("SETUP ABOUT THIS APP VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button click event for the settings gear. This will trigger our session settings view.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void AboutThisApplicationButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            ViewLogger.WriteLog("PROCESSED BUTTON CLICK FOR ABOUT THIS APPLICATION ICON CORRECTLY!", LogType.WarnLog);
            if (FulcrumConstants.FulcrumMainWindow?.InformationFlyout == null) { ViewLogger.WriteLog("ERROR! INFORMATION FLYOUT IS NULL!", LogType.ErrorLog); }
            else
            {
                // Toggle the information pane
                bool IsOpen = FulcrumConstants.FulcrumMainWindow.InformationFlyout.IsOpen;
                FulcrumConstants.FulcrumMainWindow.InformationFlyout.IsOpen = !IsOpen;
                ViewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR ABOUT THIS APP FLYOUT OK!", LogType.InfoLog);
            }
        }
    }
}
