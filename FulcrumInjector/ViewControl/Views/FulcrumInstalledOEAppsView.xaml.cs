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
using FulcrumInjector.ViewControl.ViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Views
{
    /// <summary>
    /// Interaction logic for FulcrumInstalledOEAppsView.xaml
    /// </summary>
    public partial class FulcrumInstalledOeAppsView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledOeAppsViewLogger")) ?? new SubServiceLogger("InstalledOeAppsViewLogger");

        // ViewModel object to bind onto
        public FulcrumInstalledOeAppsViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new OE App status view object
        /// </summary>
        public FulcrumInstalledOeAppsView()
        {
            // Init component. Build new VM object
            InitializeComponent();
            this.ViewModel = new FulcrumInstalledOeAppsViewModel();
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumInstalledOeAppsView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Configure pipe instances here.
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR OE APP INSTALLS OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
