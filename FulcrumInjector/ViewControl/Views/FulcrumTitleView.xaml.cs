using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.ViewControl.ViewModels;
using MahApps.Metro.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Views
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

        // Flyout Models for the settings and debugging views
        public Flyout SettingsFlyout { get; private set; }
        public Flyout DebuggingFlyout { get; private set;}

        /// <summary>
        /// Configures flyouts for our view controls. 
        /// </summary>
        /// <param name="Settings">View for settings</param>
        /// <param name="Debug">View for debugging</param>
        public bool SetFlyoutBindings(Flyout Settings = null, Flyout Debug = null)
        {
            // Check if they're both null. If so log warning and return. 
            if (Settings == null && Debug == null)
            {
                ViewLogger.WriteLog("WARNING: BOTH FLYOUT OBJECTS WERE NULL! NOT SETTING THEM!", LogType.WarnLog);
                return false;
            }

            // Set flyouts here.
            if (Settings != null)
            {
                this.SettingsFlyout = Settings;
                ViewLogger.WriteLog("STORED NEW SETTINGS FLYOUT VALUE OK!", LogType.InfoLog);
            }
            if (Debug != null)
            {
                this.DebuggingFlyout = Debug;
                ViewLogger.WriteLog("STORED NEW DEBUG FLYOUT VALUE OK!", LogType.InfoLog);
            }

            // Log and return 
            ViewLogger.WriteLog("DEBUGGING AND SETTINGS FLYOUT VALUES PROCESSED FROM METHOD ARGS AND STORED IF NEEDED OK!");
            return true;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumTitleView()
        {
            InitializeComponent();
            ViewModel = new FulcrumTitleViewModel();
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
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Button click event for the settings gear. This will trigger our session settings view.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void SettingsGearButton_Click(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            ViewLogger.WriteLog("PROCESSED BUTTON CLICK FOR SETTINGS ICON CORRECTLY!", LogType.WarnLog);
            if (this.SettingsFlyout == null) { ViewLogger.WriteLog("ERROR! SETTINGS FLYOUT IS NULL!", LogType.ErrorLog); } 
            else
            {
                this.SettingsFlyout.IsOpen = !this.SettingsFlyout.IsOpen;
                ViewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR SETTINGS FLYOUT OK!", LogType.InfoLog);
            }
        }
        /// <summary>
        /// Button click event for the debug/feedback button hit. This will show the debug pane
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void BugFeedbackButton_Click(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            ViewLogger.WriteLog("PROCESSED BUTTON CLICK FOR DEBUG/FEEDBACK ICON CORRECTLY!", LogType.WarnLog);
            if (this.DebuggingFlyout == null) { ViewLogger.WriteLog("ERROR! DEBUGGIN FLYOUT IS NULL!", LogType.ErrorLog); }
            else
            {
                this.DebuggingFlyout.IsOpen = !this.DebuggingFlyout.IsOpen;
                ViewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR DEBUGGING FLYOUT OK!", LogType.InfoLog);
            }
        }
    }
}
