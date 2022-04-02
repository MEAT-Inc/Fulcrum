using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using Newtonsoft.Json;
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
            // Build new ViewModel object
            InitializeComponent();
            this.ViewModel = FulcrumViewConstants.FulcrumSettingsPaneViewModel ?? new FulcrumSettingsPaneViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
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

            // Configure Settings instances here.
            Dispatcher.Invoke(() => { this.ViewModel.PopulateAppSettingJsonViewer(JsonSettingsViewEditor); });
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND LOGGING TARGETS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Triggered when an event is processed by a setting value changed.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void SettingValueChanged_OnTrigger(object Sender, RoutedEventArgs E)
        {
            // Log processing content value here.
            this.ViewLogger.WriteLog("PROCESSING SETTING CONTENT VALUE CHANGED EVENT!", LogType.InfoLog);

            // Get our type values and context here. Then modify the new value of our setting as needed.
            Control SendingControl = (Control)Sender;
            SettingsEntryModel SenderContext = SendingControl.DataContext as SettingsEntryModel;
            if (SendingControl is ComboBox SendingComboBox)
            {
                // Log information, find the selected object value
                this.ViewLogger.WriteLog("SETTING UP NEW ARRAY VALUES FOR COMBOBOX!", LogType.InfoLog);
                string SelectedValue = SendingComboBox.SelectedItem.ToString();

                // Get Array of values
                List<string> SettingValueOptions = ((string[])SenderContext.SettingValue).ToList();
                SettingValueOptions.Remove(SelectedValue); SettingValueOptions.Insert(0, SelectedValue);
                this.ViewLogger.WriteLog("UPDATED SETTINGS VALUE OK FOR COMBOBOX ITEM!", LogType.InfoLog);
            }
            
            // Apply our setting value here.
            if (SenderContext != null) this.ViewModel.SaveSettingValue(SenderContext); 
            else this.ViewLogger.WriteLog("FAILED TO BUILD CONTEXT FOR OUR SETTING OBJECT!", LogType.ErrorLog);
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
            this.ViewLogger.WriteLog("SAVING SETTINGS VALUES MANUALLY NOW...", LogType.InfoLog);

            // Store new values from our ViewModel onto the share and into JSON 
            ValueSetters.SetValue("FulcrumUserSettings", this.ViewModel.SettingsEntrySets);
            this.ViewModel.SettingsEntrySets = FulcrumSettingsShare.GenerateSettingsModels();
            this.ViewLogger.WriteLog("STORED NEW SETTINGS VALUES WITHOUT ISSUE!", LogType.InfoLog);

            // Change Color and Set to Saved! on the content here.
            string OriginalContent = SendButton.Content.ToString(); var OriginalBackground = SendButton.Background;
            SendButton.Content = "Saved OK!"; SendButton.Background = Brushes.DarkGreen;
            Task.Run(() =>
            {
                // Wait 3 Seconds. Then reset content.
                Thread.Sleep(3000);
                Dispatcher.Invoke(() => SendButton.Content = OriginalContent);
                Dispatcher.Invoke(() => SendButton.Background = OriginalBackground);
                this.ViewLogger.WriteLog("RESET CONTENT AND COLOR OF SENDING SAVE BUTTON OK!", LogType.TraceLog);
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
