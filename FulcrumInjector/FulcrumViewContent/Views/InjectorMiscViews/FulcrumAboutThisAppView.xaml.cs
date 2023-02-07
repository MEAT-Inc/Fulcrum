using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorMiscViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorMiscViews
{
    /// <summary>
    /// Interaction logic for AboutThisAppView.xaml
    /// </summary>
    public partial class FulcrumAboutThisAppView : UserControl
    {
        // Logger object.
      /// <summary>
      // private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("AboutThisAppViewLogger", LoggerActions.SubServiceLogger);
      /// </summary>

        // ViewModel object to bind onto
        public FulcrumAboutThisAppViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view showing title information and the text for the version
        /// </summary>
        public FulcrumAboutThisAppView()
        {
            InitializeComponent();
            this.ViewModel = new FulcrumAboutThisAppViewModel();
        //    this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumAboutThisAppView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            ViewModel.SetupViewControl(this);
            DataContext = ViewModel;

            // Log booted title view
          //  this.ViewLogger.WriteLog("SETUP ABOUT THIS APP VIEW CONTROL COMPONENT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------
    }
}
