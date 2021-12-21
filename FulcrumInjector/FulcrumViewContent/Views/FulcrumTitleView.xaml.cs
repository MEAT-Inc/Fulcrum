using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

            // Log booted title view
            this.ViewLogger.WriteLog("SETUP TITLE VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
        }
    }
}
