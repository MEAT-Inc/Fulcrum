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
    public partial class FulcrumInstalledOEAppsView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InstalledOEAppsViewLogger")) ?? new SubServiceLogger("InstalledOEAppsViewLogger");

        // ViewModel object to bind onto
        public FulcrumPipeStatusViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Inits a new view for our installed OE Apps.
        /// </summary>
        public FulcrumInstalledOEAppsView()
        {
            InitializeComponent();
        }

        private void FulcrumInstalledOEAppsView_OnLoaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
