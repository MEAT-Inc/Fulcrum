using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewSupport;
using FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using MahApps.Metro.Controls;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumHamburgerCoreView.xaml
    /// </summary>
    public partial class FulcrumHamburgerCoreView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content and our navigation animator
        private readonly SharpLogger _viewLogger;
        private readonly HamburgerNavService NavService;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        public FulcrumHamburgerCoreViewModel ViewModel { get; set; }   

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for Hamburger Core output view
        /// </summary>
        public FulcrumHamburgerCoreView()
        {
            // Spawn a new logger and setup our view model
            this.ViewModel = new FulcrumHamburgerCoreViewModel(this);
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Initialize new UI Component
            InitializeComponent();

            // Configure new Navigation Service helper
            this.NavService = new HamburgerNavService();
            this.InjectorHamburgerMenu.Content = NavService.NavigationFrame;
            this.NavService.Navigated += this.NavigationServiceEx_OnNavigated;
            this._viewLogger.WriteLog("CONFIGURED NEW NAV SERVICE FOR OUR HAMBURGER CORE OBJECT OK!", LogType.InfoLog);

            // Setup our data context and log information out
            // this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR FULCRUM HAMBURGER CORE OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }
        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumHamburgerCoreView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup Menu icon objects here.
            this.InjectorHamburgerMenu.ItemsSource = this.ViewModel.SetupHamburgerMenuItems();
            this.InjectorHamburgerMenu.OptionsItemsSource =  this.ViewModel.SetupHamburgerOptionItems();
            this._viewLogger.WriteLog("SETUP AND STORED NEW MENU ENTRIES ON THE VIEW OK!", LogType.InfoLog);

            // Log built view contents ok
            this.InjectorHamburgerMenu.SelectedIndex = 0;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// When a view is updated, run this method. Check the type of the menu objct, and then if possible, invoke changed view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InjectorHamburgerMenu_OnItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs e)
        {
            // Navigate assuming it's a type of nav menu item and the menu item can navigate
            if (e.InvokedItem is not FulcrumNavMenuItem BuiltItemObject || !BuiltItemObject.IsNavigation) return;

            // Navigate here and 
            this.NavService.Navigate(BuiltItemObject.NavUserControlType, BuiltItemObject.NavViewModelType);
            this._viewLogger.WriteLog($"NAVIGATED FROM SELECTED MENU ITEM TO A NEW CONTROL VIEW CORRECTLY!", LogType.TraceLog);
        }
        /// <summary>
        /// Event when view content is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigationServiceEx_OnNavigated(object sender, NavigationEventArgs e)
        {
            // Select the menu item and the option item here
            this.InjectorHamburgerMenu.SelectedItem = this.InjectorHamburgerMenu
                .Items
                .OfType<FulcrumNavMenuItem>()
                .FirstOrDefault(MenuObj => MenuObj.NavUserControlType == e.Content?.GetType());
            this._viewLogger.WriteLog($"BOUND SELECTED MENU ITEM TO {this.InjectorHamburgerMenu.SelectedIndex}", LogType.TraceLog);
 
            // Set options items
            this.InjectorHamburgerMenu.SelectedOptionsItem = this.InjectorHamburgerMenu
                .OptionsItems
                .OfType<FulcrumNavMenuItem>()
                .FirstOrDefault(MenuObj => MenuObj.NavUserControlType == e.Content?.GetType());
            this._viewLogger.WriteLog($"BOUND SELECTED OPTIONS ITEM TO {this.InjectorHamburgerMenu.SelectedOptionsIndex}", LogType.TraceLog);
        }
    }
}
