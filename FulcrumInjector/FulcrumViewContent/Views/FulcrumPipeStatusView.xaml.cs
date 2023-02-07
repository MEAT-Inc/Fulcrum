﻿using FulcrumInjector.FulcrumViewContent.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FulcrumInjector.FulcrumViewContent.Views
{
    /// <summary>
    /// Interaction logic for FulcrumPipeStatusView.xaml
    /// </summary>
    public partial class FulcrumPipeStatusView : UserControl
    {
        // Logger object.
     //   private SubServiceLogger ViewLogger => (SubServiceLogger)LoggerQueue.SpawnLogger("PipeStatusViewLogger", LoggerActions.SubServiceLogger);

        // ViewModel object to bind onto
        public FulcrumPipeStatusViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumPipeStatusView()
        {
            // Init component. Build new VM object
            InitializeComponent();
            Dispatcher.InvokeAsync(() =>
            {
                // Build new view model object
                this.ViewModel = new FulcrumPipeStatusViewModel();

                // Store default values for our pipe states
                this.ViewModel.ReaderPipeState = "Loading...";
                this.ViewModel.WriterPipeState = "Loading...";
            });

            // Log built new content OK
          //  this.ViewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumPipeStatusView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;

            // Configure pipe instances here.
            Dispatcher.InvokeAsync(() =>
            {
                this.ViewModel.SetupPipeStateWatchdogs();
             //   this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES AND WATCHDOGS OK!", LogType.InfoLog);
            });
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
