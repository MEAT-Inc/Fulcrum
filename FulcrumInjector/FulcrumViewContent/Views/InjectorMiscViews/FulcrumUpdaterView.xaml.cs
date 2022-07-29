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
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorMiscViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

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

            // Log booted title view
            this.ViewLogger.WriteLog("SETUP UPDATER VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------
    }
}
