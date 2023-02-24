using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xaml;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using Markdig;
using SharpLogging;
using Markdown = Markdig.Wpf.Markdown;
using XamlReader = System.Windows.Markup.XamlReader;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for UpdaterView.xaml
    /// </summary>
    public partial class FulcrumUpdaterView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumUpdaterViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumUpdaterView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = new FulcrumUpdaterViewModel(this);

            // Initialize new UI Component
            InitializeComponent();
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumUpdaterView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new data context for our view model
            this.DataContext = this.ViewModel;

            // See if we need to open the updater view
            this.UpdateReleaseNotesContents();
            if (!this.ViewModel.UpdateReady) {
                this._viewLogger.WriteLog("SETUP UPDATER VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
                return;
            }

            // Log ready to show updates and build our XAML content output
            this._viewLogger.WriteLog("SHOWING UPDATE WINDOW SINCE AN UPDATE IS READY!", LogType.InfoLog);
            FulcrumConstants.FulcrumMainWindow.AppUpdatesFlyout.IsOpen = true;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Updates the release notes viewer with the latest release notes
        /// </summary>
        private void UpdateReleaseNotesContents()
        {
            // Build in the release notes contents here
            var XamlReleaseNotes = Markdown.ToXaml(
                this.ViewModel.GitHubUpdateHelper.LatestInjectorReleaseNotes,
                new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build()
            );

            // Now append the contents of the markdown into our output viewer
            using var MemStream = new MemoryStream(Encoding.UTF8.GetBytes(XamlReleaseNotes));
            using (var XamlToXmlReader = new XamlXmlReader(MemStream, new MarkdownXamlSchemaContext()))
                if (XamlReader.Load(XamlToXmlReader) is FlowDocument OutputDocument)
                    this.ReleaseNotesViewer.Document = OutputDocument;

            // Log done building release notes
            this._viewLogger.WriteLog("RELEASE NOTES FOR UPDATER WERE BUILT AND ARE BEING SHOWN NOW!", LogType.InfoLog);
        }
        /// <summary>
        /// Method to pop open hyperlinks from the converted markdown document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenHyperlink(object sender, ExecutedRoutedEventArgs e) => Process.Start(e.Parameter.ToString());
        /// <summary>
        /// Button click command to execute a new update install request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartUpdateFlyoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Invoke a download for our updater
            this._viewLogger.WriteLog("PULLING NEWEST INJECTOR RELEASE USING OUR VIEW MODEL HELPERS NOW...", LogType.WarnLog);
            string OutputAssetFile = this.ViewModel.InvokeInjectorDownload();
            this._viewLogger.WriteLog($"ASSET PATH PULLED IN: {OutputAssetFile}");

            // Now request a new install routine from the view model.
            this._viewLogger.WriteLog("BOOTING NEW INSTALLER FOR THE FULCRUM INJECTOR NOW...", LogType.InfoLog);
            this.ViewModel.InstallInjectorRelease(OutputAssetFile);
        }
        /// <summary>
        /// Button click event for the updates view
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void ToggleApplicationUpdateReadyView_OnClick(object Sender, RoutedEventArgs E)
        {
            // Log processed and show if we have to.
            _viewLogger.WriteLog("PROCESSED BUTTON CLICK FOR APP UPDATES VIEW", LogType.WarnLog);
            if (FulcrumConstants.FulcrumMainWindow?.AppUpdatesFlyout == null) { _viewLogger.WriteLog("ERROR! UPDATES FLYOUT IS NULL!", LogType.ErrorLog); }
            else
            {
                // Toggle the information pane
                bool IsOpen = FulcrumConstants.FulcrumMainWindow.AppUpdatesFlyout.IsOpen;
                FulcrumConstants.FulcrumMainWindow.AppUpdatesFlyout.IsOpen = !IsOpen;
                _viewLogger.WriteLog("PROCESSED VIEW TOGGLE REQUEST FOR APP UPDATES OK!", LogType.InfoLog);
            }
        }
    }
}
