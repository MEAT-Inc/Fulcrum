using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Xaml;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorMiscViewModels;
using FulcrumInjector.FulcrumViewSupport.DataContentHelpers;
using Markdig;
using Markdig.Syntax;
using Markdig.Wpf;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Markdown = Markdig.Wpf.Markdown;
using XamlReader = System.Windows.Markup.XamlReader;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for UpdaterView.xaml
    /// </summary>
    public partial class FulcrumUpdaterView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("UpdaterAppViewLogger")) ?? new SubServiceLogger("UpdaterAppViewLogger");

        // ViewModel object to bind onto
        public FulcrumUpdaterViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumUpdaterView()
        {
            InitializeComponent();
            this.ViewModel = new FulcrumUpdaterViewModel();
            this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }


        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumUpdaterView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            ViewModel.SetupViewControl(this);
            DataContext = ViewModel;

            // See if we need to open the updater view
            this.UpdateReleaseNotesContents();
            if (!this.ViewModel.UpdateReady) {
                this.ViewLogger.WriteLog("SETUP UPDATER VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
                return;
            }

            // Log ready to show updates and build our XAML content output
            this.ViewLogger.WriteLog("SHOWING UPDATE WINDOW SINCE AN UPDATE IS READY!", LogType.InfoLog);
            FulcrumConstants.InjectorMainWindow.AppUpdatesFlyout.IsOpen = true;
        }

        // --------------------------------------------------------------------------------------------------------------------------

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
            this.ViewLogger.WriteLog("RELEASE NOTES FOR UPDATER WERE BUILT AND ARE BEING SHOWN NOW!", LogType.InfoLog);
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
            this.ViewLogger.WriteLog("PULLING NEWEST INJECTOR RELEASE USING OUR VIEW MODEL HELPERS NOW...", LogType.WarnLog);
            string OutputAssetFile = this.ViewModel.InvokeInjectorDownload();
            this.ViewLogger.WriteLog($"ASSET PATH PULLED IN: {OutputAssetFile}");

            // Now request a new install routine from the view model.
            // TODO: BUILD LOGIC TO RUN INSTALLER
        }
    }
}
