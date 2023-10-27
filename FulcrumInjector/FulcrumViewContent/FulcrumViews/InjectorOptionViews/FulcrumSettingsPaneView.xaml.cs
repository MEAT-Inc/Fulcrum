using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumJsonSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorOptionViews
{
    /// <summary>
    /// Interaction logic for FulcrumSettingsPaneView.xaml
    /// </summary>
    public partial class FulcrumSettingsPaneView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        public FulcrumSettingsPaneViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumSettingsPaneView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumSettingsPaneViewModel ?? new FulcrumSettingsPaneViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Setup our data context and log information out
            // this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumSettingsPaneView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Populate our settings values here and refresh content from the injector constants
            this.ViewModel.PopulateAppSettingJsonViewer(this.JsonSettingsViewEditor);
            this.ViewModel.SettingsEntrySets = new (FulcrumConstants.FulcrumSettings.Values);
            this._viewLogger.WriteLog("BUILT AND LOADED IN SESSION SETTINGS FOR THIS VIEW CORRECTLY!");
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Triggered when an event is processed by a setting value changed.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void SettingValueChanged_OnTrigger(object Sender, RoutedEventArgs E)
        {
            // Log processing content value here.
            this._viewLogger.WriteLog("PROCESSING SETTING CONTENT VALUE CHANGED EVENT!", LogType.InfoLog);

            // Get our type values and context here. Then modify the new value of our setting as needed.
            Control SendingControl = (Control)Sender;
            FulcrumSettingEntryModel SenderContext = SendingControl.DataContext as FulcrumSettingEntryModel;
            if (SendingControl is ComboBox SendingComboBox)
            {
                // Log information, find the selected object value
                this._viewLogger.WriteLog("SETTING UP NEW ARRAY VALUES FOR COMBOBOX!", LogType.InfoLog);
                string SelectedValue = SendingComboBox.SelectedItem.ToString();

                // Get Array of values
                List<string> SettingValueOptions = ((string[])SenderContext.SettingValue).ToList();
                SettingValueOptions.Remove(SelectedValue); SettingValueOptions.Insert(0, SelectedValue);
                this._viewLogger.WriteLog("UPDATED SETTINGS VALUE OK FOR COMBOBOX ITEM!", LogType.InfoLog);
            }
            
            // Apply our setting value here.
            if (SenderContext != null) this.ViewModel.SaveSettingValue(SenderContext); 
            else this._viewLogger.WriteLog("FAILED TO BUILD CONTEXT FOR OUR SETTING OBJECT!", LogType.ErrorLog);
        }
        /// <summary>
        /// Force saves all settings values from our view model onto the settings share for this application
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void SaveSettingsButton_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log information, save all settings objects to our JSON file now.
            Button SendButton = (Button)Sender;
            this._viewLogger.WriteLog("SAVING SETTINGS VALUES MANUALLY NOW...", LogType.InfoLog);

            // Store new values from our ViewModel onto the share and into JSON 
            Task.Run(() =>
            {
                // Save all settings objects into our settings file
                this.ViewModel.SaveAllSettings();
                
                // Change Color and Set to Saved! on the content here.
                string OriginalContent = SendButton.Content.ToString(); var OriginalBackground = SendButton.Background;
                SendButton.Content = "Saved OK!"; SendButton.Background = Brushes.DarkGreen;
                Task.Run(() =>
                {
                    // Wait 3 Seconds. Then reset content.
                    Thread.Sleep(3000);
                    Dispatcher.Invoke(() => SendButton.Content = OriginalContent);
                    Dispatcher.Invoke(() => SendButton.Background = OriginalBackground);
                    this._viewLogger.WriteLog("RESET CONTENT AND COLOR OF SENDING SAVE BUTTON OK!", LogType.TraceLog);
                });
            });
        }
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
            this._viewLogger.WriteLog("OPENING JSON VIEWER FLYOUT NOW...", LogType.TraceLog);
            this.ViewModel.PopulateAppSettingJsonViewer(JsonSettingsViewEditor);
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
            this._viewLogger.WriteLog("REFRESHING JSON CONTENTS NOW...", LogType.TraceLog);
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
            this._viewLogger.WriteLog("SAVING NEW JSON CONTENTS NOW...", LogType.TraceLog);
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
            this._viewLogger.WriteLog("CLOSING JSON VIEWER FLYOUT NOW...", LogType.TraceLog);
            this.JsonViewerFlyout.IsOpen = false;
        }
        /// <summary>
        /// Passes the scroll events from our listbox content up to the parent scroll viewer so scrolling operations work
        /// </summary>
        /// <param name="sender">Sending control for this event</param>
        /// <param name="e">Event args fired along with this action</param>
        private void SettingsListBoxView_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Set the event to handled and raise the event provided in on the parent object
            e.Handled = true;
            int NewDelta = (int)this.SettingsScrollViewer.VerticalOffset - (int)(e?.Delta);
            this.SettingsScrollViewer.ScrollToVerticalOffset(NewDelta);
        }
    }
}
