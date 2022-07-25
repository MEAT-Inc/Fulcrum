using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using MahApps.Metro.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for TitleTextAndQuickActions.xaml
    /// </summary>
    public partial class FulcrumTitleView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("TitleViewLogger")) ?? new SubServiceLogger("TitleViewLogger");

        // ViewModel object to bind onto
        public FulcrumTitleViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumTitleView()
        {
            InitializeComponent();
            this.Dispatcher.InvokeAsync(() => this.ViewModel = new FulcrumTitleViewModel());
            this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumTitleView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            ViewModel.SetupViewControl(this);
            DataContext = ViewModel;

            // Log booted title view
            this.ViewLogger.WriteLog("SETUP TITLE VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        // Flyout Models for the settings and debugging views
        public Flyout InformationFlyout { get; private set; }

        /// <summary>
        /// Configures flyouts for our view controls. 
        /// </summary>
        /// <param name="Settings">View for settings</param>
        /// <param name="Debug">View for debugging</param>
        public bool SetFlyoutBindings(Flyout InformationFlyout, Button CloseButton)
        {
            // Store the flyout here and apply the button actions to it
            this.InformationFlyout = InformationFlyout;
            CloseButton.Click += AboutThisApplicationButton_OnClick;
            ViewLogger.WriteLog("STORED NEW APP INFORMATION FLYOUT VALUE OK!", LogType.InfoLog);
            ViewLogger.WriteLog("STORED NEW APP INFORMATION CLOSING BUTTON COMMAND VALUE OK!", LogType.InfoLog);

            // Log and return 
            ViewLogger.WriteLog("INFORMATION FLYOUT AND CONTROL BUTTONS HAVE BEEN SETUP AND BOUND OK!");
            return true;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button click event for the settings gear. This will trigger our session settings view.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void AboutThisApplicationButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            ViewLogger.WriteLog("PROCESSED BUTTON CLICK FOR ABOUT THIS APPLICATION ICON CORRECTLY!", LogType.WarnLog);
            if (this.InformationFlyout == null) { ViewLogger.WriteLog("ERROR! INFORMATION FLYOUT IS NULL!", LogType.ErrorLog); }
            else
            {
                // Toggle the information pane
                this.InformationFlyout.IsOpen = !this.InformationFlyout.IsOpen;
                ViewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR ABOUT THIS APP FLYOUT OK!", LogType.InfoLog);
            }
        }

        /// <summary>
        /// Toggles which version information is currently shown on the title view
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void ToggleVersionInfoButton_OnClick(object Sender, RoutedEventArgs E)
        {

        }
    }
}
