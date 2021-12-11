using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.ViewControl.ViewModels;
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


        /// <summary>
        /// Button click event for the settings gear. This will trigger our session settings view.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void SettingsGearButton_Click(object Sender, RoutedEventArgs E)
        {
            // TODO: BUILD LOGIC TO SHOW SETTINGS!
            ViewLogger.WriteLog("PROCESSED BUTTON CLICK FOR SETTINGS ICON CORRECTLY!", LogType.WarnLog);
        }
        /// <summary>
        /// Button click event for the debug/feedback button hit. This will show the debug pane
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void BugFeedbackButton_Click(object Sender, RoutedEventArgs E)
        {
            // TODO: BUILD LOGIC TO SHOW DEBUG PANE!
            ViewLogger.WriteLog("PROCESSED BUTTON CLICK FOR DEBUG/FEEDBACK ICON CORRECTLY!", LogType.WarnLog);
        }
    }
}
