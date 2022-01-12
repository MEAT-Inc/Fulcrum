using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorOptionViews
{
    /// <summary>
    /// Interaction logic for FulcrumSettingsPaneView.xaml
    /// </summary>
    public partial class FulcrumSettingsPaneView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SettingsViewLogger")) ?? new SubServiceLogger("SettingsViewLogger");

        // ViewModel object to bind onto
        public FulcrumSettingsPaneViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumSettingsPaneView()
        {
            // Init component. Build new VM object
            InitializeComponent();
            this.ViewModel = InjectorConstants.FulcrumSettingsPaneViewModel ?? new FulcrumSettingsPaneViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);

            // Find the global color sheet and store values for it.
            var CurrentMerged = Application.Current.Resources.MergedDictionaries;
            this.Resources["AppColorTheme"] = CurrentMerged.FirstOrDefault(Dict => Dict.Source.ToString().Contains("AppColorTheme"));
            ViewLogger.WriteLog($"SETUP MAIN COLOR THEME FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumSettingsPaneView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Configure pipe instances here.
            Dispatcher.Invoke(() => this.ViewModel.PopulateAppSettingJsonViewer(JsonSettingsViewEditor));
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens the JSON content view object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenJsonViewerFlyoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Check if opened or closed
            if (this.JsonViewerFlyout.IsOpen) { this.CloseJsonViewerFlyoutButton_OnClick(sender, e); return; }

            // Toggle view visibility
            ViewLogger.WriteLog("OPENING JSON VIEWER FLYOUT NOW...", LogType.TraceLog);
            this.JsonViewerFlyout.IsOpen = true;
        }
        /// <summary>
        /// Updates the application json content in the editor view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReloadJsonContentFlyoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Repopulate the values here.
            ViewLogger.WriteLog("REFRESHING JSON CONTENTS NOW...", LogType.TraceLog);
            this.ViewModel.PopulateAppSettingJsonViewer(this.JsonSettingsViewEditor);
        }
        /// <summary>
        /// Updates the application json content in the editor view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveJsonContentFlyoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Repopulate the values here.
            ViewLogger.WriteLog("SAVING NEW JSON CONTENTS NOW...", LogType.TraceLog);
            this.ViewModel.SaveAppSettingJsonAsConfig(this.JsonSettingsViewEditor);
        }
        /// <summary>
        /// Closes the viewer for the flyout containing the JSON file information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseJsonViewerFlyoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Check if currently closed or not.
            if (!this.JsonViewerFlyout.IsOpen) { this.OpenJsonViewerFlyoutButton_OnClick(sender, e); return; }

            // Toggle view visibility
            ViewLogger.WriteLog("CLOSING JSON VIEWER FLYOUT NOW...", LogType.TraceLog);
            this.JsonViewerFlyout.IsOpen = false;
        }
    }
}
