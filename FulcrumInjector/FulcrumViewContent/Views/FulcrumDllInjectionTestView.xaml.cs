﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for TestDllInjectionView.xaml
    /// </summary>
    public partial class FulcrumDllInjectionTestView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorTestViewLogger")) ?? new SubServiceLogger("InjectorTestViewLogger");

        // ViewModel object to bind onto
        public FulcrumDllInjectionTestViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds new logic for a view which tests our DLL Injection routines
        /// </summary>
        public FulcrumDllInjectionTestView()
        {
            // Build new view model object
            InitializeComponent();
            this.ViewModel = new FulcrumDllInjectionTestViewModel();
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
                this.ViewModel.InjectorTestResult = "Testing...";
                ViewLogger.WriteLog("ATTEMPTING INJECTOR LOGIC INJECTION ON THE VIEWMODEL NOW...", LogType.WarnLog);

                // Run the injection test here on a Dispatched thread 
                this.ViewModel.InjectionLoadPassed = this.ViewModel.TestInjectorDllLoading(out string ResultOutput);
                if (!this.ViewModel.InjectionLoadPassed) { ViewLogger.WriteLog($"FAILED TO INJECT DLL INTO THE SYSTEM! SEE LOG FILES FOR MORE INFORMATION!", LogType.ErrorLog); }
                else { ViewLogger.WriteLog($"INJECTION PASSED OK! READY TO USE WITH OE APPLICATIONS!", LogType.InfoLog); }

                // Set Value on the View now.
                this.ViewModel.InjectorTestResult = ResultOutput;
            }
            catch (Exception Ex)
            {
                // Log the failure here.
                ViewLogger.WriteLog("----------------------------------------------", LogType.FatalLog);
                ViewLogger.WriteLog("FAILED TO LOAD OUR DLL!", LogType.ErrorLog);
                ViewLogger.WriteLog($"EXCEPTION THROWN: {Ex.Message}", LogType.ErrorLog);
                ViewLogger.WriteLog("THIS IS A FATAL ISSUE!", LogType.ErrorLog);
                ViewLogger.WriteLog("----------------------------------------------", LogType.FatalLog);

                // Store output
                this.ViewModel.InjectorTestResult = "Load Failure!";
                ViewLogger.WriteLog($"EXCEPTION THROWN: {Ex.Message}", LogType.ErrorLog);
                ViewLogger.WriteLog("EXCEPTION CONTENTS ARE BEING LOGGED TO FILE NOW", Ex);
            }
        }
    }
}