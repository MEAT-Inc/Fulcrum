using System;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews
{
    /// <summary>
    /// Interaction logic for TestDllInjectionView.xaml
    /// </summary>
    public partial class FulcrumDllInjectionTestView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumDllInjectionTestViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view which tests our DLL Injection routines
        /// </summary>
        public FulcrumDllInjectionTestView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = new FulcrumDllInjectionTestViewModel(this);

            // Initialize new UI Component
            InitializeComponent();

            // Setup our data context and log information out
            // this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE INJECTION TEST VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Tests the injection for the DLL on this application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestInjectionButton_Click(object sender, RoutedEventArgs e)
        { 
            try
            {
                // Clear out text box, run the test, and log the output
                TestInjectionButton.IsEnabled = false;
                this.ViewModel.InjectorTestResult = "Working...";
                this._viewLogger.WriteLog("ATTEMPTING INJECTOR LOGIC INJECTION ON THE VIEWMODEL NOW...", LogType.WarnLog);

                // Set View to show the Injector Output View for the hamburger main menu
                // var MenuItem = InjectorConstants.FulcrumHamburgerCoreViewModel.FulcrumMenuEntries
                //     .FirstOrDefault(MenuObj => MenuObj.MenuViewType == typeof(FulcrumDllOutputLogView).FullName);
                // InjectorConstants.FulcrumHamburgerCoreView.InjectorHamburgerMenu.SelectedIndex =
                //     InjectorConstants.FulcrumHamburgerCoreViewModel.FulcrumMenuEntries.ToList().IndexOf(MenuItem);

                // Run the injection test here on a Dispatched thread 
                string ResultOutput = string.Empty;
                this.ViewModel.InjectionLoadPassed = this.ViewModel.TestInjectorDllLoading(out ResultOutput);
                if (!this.ViewModel.InjectionLoadPassed) { this._viewLogger.WriteLog($"FAILED TO INJECT DLL INTO THE SYSTEM! SEE LOG FILES FOR MORE INFORMATION!", LogType.ErrorLog); }
                else this._viewLogger.WriteLog($"INJECTION PASSED OK! READY TO USE WITH OE APPLICATIONS!", LogType.InfoLog);

                // Set Value on the View now.
                TestInjectionButton.IsEnabled = false;
                TestInjectionButton.Content = "Test Injection";
                this.ViewModel.InjectorTestResult = ResultOutput;
            }
            catch (Exception Ex)
            {
                // Log the failure here.
                TestInjectionButton.IsEnabled = true;
                TestInjectionButton.Content = "Test Injection";
                this._viewLogger.WriteLog("----------------------------------------------", LogType.FatalLog);
                this._viewLogger.WriteLog("FAILED TO LOAD OUR DLL!", LogType.ErrorLog);
                this._viewLogger.WriteLog($"EXCEPTION THROWN: {Ex.Message}", LogType.ErrorLog);
                this._viewLogger.WriteLog("THIS IS A FATAL ISSUE!", LogType.ErrorLog);
                this._viewLogger.WriteLog("----------------------------------------------", LogType.FatalLog);

                // Store output
                this.ViewModel.InjectorTestResult = "Load Failure!";
                this._viewLogger.WriteLog($"EXCEPTION THROWN: {Ex.Message}", LogType.ErrorLog);
                this._viewLogger.WriteException("EXCEPTION CONTENTS ARE BEING LOGGED TO FILE NOW", Ex);
            }
        }
    }
}
