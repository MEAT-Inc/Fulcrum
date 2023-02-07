using FulcrumInjector.FulcrumViewContent.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for TestDllInjectionView.xaml
    /// </summary>
    public partial class FulcrumDllInjectionTestView : UserControl
    {
        // Logger object.
      //  private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("InjectorTestViewLogger", LoggerActions.SubServiceLogger);

        // ViewModel object to bind onto
        public FulcrumDllInjectionTestViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view which tests our DLL Injection routines
        /// </summary>
        public FulcrumDllInjectionTestView()
        {
            // Initialize new UI Component
            InitializeComponent();

            // Build new view model object
            Dispatcher.InvokeAsync(() => this.ViewModel = new FulcrumDllInjectionTestViewModel());
         //   this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumInjectorTestView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            ViewModel.SetupViewControl(this);
            DataContext = this.ViewModel;
        }

        // --------------------------------------------------------------------------------------------------------------------------

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
             //   ViewLogger.WriteLog("ATTEMPTING INJECTOR LOGIC INJECTION ON THE VIEWMODEL NOW...", LogType.WarnLog);

                // Set View to show the Injector Output View for the hamburger main menu
                // var MenuItem = InjectorConstants.FulcrumHamburgerCoreViewModel.FulcrumMenuEntries
                //     .FirstOrDefault(MenuObj => MenuObj.MenuViewType == typeof(FulcrumDllOutputLogView).FullName);
                // InjectorConstants.FulcrumHamburgerCoreView.InjectorHamburgerMenu.SelectedIndex =
                //     InjectorConstants.FulcrumHamburgerCoreViewModel.FulcrumMenuEntries.ToList().IndexOf(MenuItem);

                // Run the injection test here on a Dispatched thread 
                string ResultOutput = string.Empty;
                this.ViewModel.InjectionLoadPassed = this.ViewModel.TestInjectorDllLoading(out ResultOutput);
              //  if (!this.ViewModel.InjectionLoadPassed) { ViewLogger.WriteLog($"FAILED TO INJECT DLL INTO THE SYSTEM! SEE LOG FILES FOR MORE INFORMATION!", LogType.ErrorLog); }
              //  else ViewLogger.WriteLog($"INJECTION PASSED OK! READY TO USE WITH OE APPLICATIONS!", LogType.InfoLog);

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
             //   ViewLogger.WriteLog("----------------------------------------------", LogType.FatalLog);
             //   ViewLogger.WriteLog("FAILED TO LOAD OUR DLL!", LogType.ErrorLog);
             //   ViewLogger.WriteLog($"EXCEPTION THROWN: {Ex.Message}", LogType.ErrorLog);
              //  ViewLogger.WriteLog("THIS IS A FATAL ISSUE!", LogType.ErrorLog);
             //   ViewLogger.WriteLog("----------------------------------------------", LogType.FatalLog);

                // Store output
                this.ViewModel.InjectorTestResult = "Load Failure!";
             //   ViewLogger.WriteLog($"EXCEPTION THROWN: {Ex.Message}", LogType.ErrorLog);
             //   ViewLogger.WriteLog("EXCEPTION CONTENTS ARE BEING LOGGED TO FILE NOW", Ex);
            }
        }
    }
}
