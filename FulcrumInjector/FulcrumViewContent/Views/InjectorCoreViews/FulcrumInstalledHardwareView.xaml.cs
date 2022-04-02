﻿using System;
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
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534.J2534Objects;

namespace FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumInstalledHardwareView.xaml
    /// </summary>
    public partial class FulcrumInstalledHardwareView : UserControl
    {
        // Logger object.
        private SubServiceLogger ViewLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("FulcrumSessionReportingViewLogger")) ?? new SubServiceLogger("FulcrumSessionReportingViewLogger");

        // ViewModel object to bind onto
        public FulcrumInstalledHardwareViewModel ViewModel { get; set; }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumInstalledHardwareView()
        {
            // Build new ViewModel object
            InitializeComponent();
            this.ViewModel = FulcrumConstants.FulcrumInstalledHardwareViewModel ?? new FulcrumInstalledHardwareViewModel();
            ViewLogger.WriteLog($"STORED NEW VIEW OBJECT AND VIEW MODEL OBJECT FOR TYPE {this.GetType().Name} TO INJECTOR CONSTANTS OK!", LogType.InfoLog);
        }

        /// <summary>
        /// On loaded, we want to setup our new viewmodel object and populate values
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Events attached to it.</param>
        private void FulcrumInstalledHardwareView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Setup a new ViewModel
            this.ViewModel.SetupViewControl(this);
            this.DataContext = this.ViewModel;
            this.ViewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR HARDWARE INFORMATION OUTPUT OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Populates our new Device Listbox with a set of devices pulled from our DLL Entry object.
        /// </summary>
        /// <param name="SendingDllObject"></param>
        /// <param name="DllChangedEventArgs"></param>
        private void InstalledDLLsListBox_OnSelectionChanged(object SendingDllObject, SelectionChangedEventArgs DllChangedEventArgs)
        {
            // Log info, find the currently selected DLL object, cast it, and run the VM Method.
            this.ViewLogger.WriteLog("PULLING IN NEW DEVICES FOR DLL ENTRY NOW...", LogType.InfoLog);

            // Convert sender and cast our DLL object
            ListBox SendingBox = (ListBox)SendingDllObject;
            J2534Dll SelectedDLL = (J2534Dll)SendingBox.SelectedItem;

            // BUG: THIS LOG ENTRY HANGS THE WHOLE APP?
            this.ViewLogger.WriteLog(
                SelectedDLL == null ? "NO DLL ENTRY SELECTED! CLEARING" : $"DLL ENTRY PULLED: {SelectedDLL.Name}",
                LogType.TraceLog
            );

            // Log and populate devices
            Task.Run(() => {
                this.ViewModel.SelectedDLL = SelectedDLL;
                this.ViewLogger.WriteLog($"POPULATED OUR DEVICE ENTRY SET FOR DLL ENTRY WITH LONG NAME {SelectedDLL.LongName} OK!", LogType.InfoLog);
            });
        }
        /// <summary>
        /// Configures a new device selection value on the instance of our view model
        /// </summary>
        /// <param name="SendingDeviceObject"></param>
        /// <param name="DeviceChangedEventArgs"></param>
        private void InstalledDevicesListBox_OnSelectionChanged(object SendingDeviceObject, SelectionChangedEventArgs DeviceChangedEventArgs)
        {
            // Log info, find the currently selected DLL object, cast it, and run the VM Method.
            this.ViewLogger.WriteLog("PROCESSED A DEVICE SELECTION CHANGED EVENT!", LogType.InfoLog);

            // Convert sender and cast our DLL object
            ListBox SendingBox = (ListBox)SendingDeviceObject;
            string SelectedDevice = SendingBox.SelectedItem.ToString();

            // BUG: THIS LOG ENTRY HANGS THE WHOLE APP?
            this.ViewLogger.WriteLog(
                string.IsNullOrWhiteSpace(SelectedDevice) ? "NO DLL ENTRY SELECTED! CLEARING" : $"DEVICE ENTRY PULLED: {SelectedDevice}",
                LogType.TraceLog
            );

            // Log and populate devices
            this.ViewModel.SelectedDevice = SelectedDevice;
            this.ViewLogger.WriteLog($"POPULATED OUR DEVICE ENTRY NAMED {SelectedDevice} ON OUR VIEW MODEL OK!", LogType.InfoLog);
        }
    }
}
