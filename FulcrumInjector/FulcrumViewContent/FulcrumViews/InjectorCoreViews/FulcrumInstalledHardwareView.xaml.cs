using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using SharpLogging;
using SharpWrapper.J2534Objects;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews
{
    /// <summary>
    /// Interaction logic for FulcrumInstalledHardwareView.xaml
    /// </summary>
    public partial class FulcrumInstalledHardwareView : UserControl
    {
        #region Custom Events
        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        #endregion // Fields

        #region Properties

        // ViewModel object to bind onto
        internal FulcrumInstalledHardwareViewModel ViewModel { get; set; }

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new pipe status view object
        /// </summary>
        public FulcrumInstalledHardwareView()
        {
            // Spawn a new logger and setup our view model
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this.ViewModel = FulcrumConstants.FulcrumInstalledHardwareViewModel ?? new FulcrumInstalledHardwareViewModel(this);

            // Initialize new UI component instance
            InitializeComponent();

            // Setup our data context and log information out
            // this.DataContext = this.ViewModel;
            this._viewLogger.WriteLog("CONFIGURED VIEW CONTROL VALUES FOR THE INJECTION TEST VIEW OK!", LogType.InfoLog);
            this._viewLogger.WriteLog($"BUILT NEW INSTANCE FOR VIEW TYPE {this.GetType().Name} OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Populates our new Device Listbox with a set of devices pulled from our DLL Entry object.
        /// </summary>
        /// <param name="SendingDllObject"></param>
        /// <param name="DllChangedEventArgs"></param>
        private void InstalledDLLsListBox_OnSelectionChanged(object SendingDllObject, SelectionChangedEventArgs DllChangedEventArgs)
        {
            // Log info, find the currently selected DLL object, cast it, and run the VM Method.
            this._viewLogger.WriteLog("PULLING IN NEW DEVICES FOR DLL ENTRY NOW...", LogType.InfoLog);

            // Convert sender and cast our DLL object
            J2534Dll SelectedDLL = (J2534Dll)(DllChangedEventArgs.AddedItems.Count == 0 ?
                DllChangedEventArgs.RemovedItems[0] :
                DllChangedEventArgs.AddedItems[0]);

            // Clear out the Devices if Needed
            this._viewLogger.WriteLog($"DLL OBJECT BEING MODIFIED: {SelectedDLL.Name}", LogType.WarnLog);
            if (DllChangedEventArgs.RemovedItems.Contains(SelectedDLL))
            {
                // Remove the devices listed
                InstalledDevicesListBox.ItemsSource = null;
                this._viewLogger.WriteLog("CLEARED OUT OLD DLL VALUES OK!", LogType.InfoLog);
            }

            // Log and populate devices
            Task.Run(() =>
            {
                // Populate the DLL entry and let devices flow in
                this.ViewModel.SelectedDLL = SelectedDLL;
                this._viewLogger.WriteLog($"POPULATED OUR DEVICE ENTRY SET FOR DLL ENTRY WITH LONG NAME {SelectedDLL.LongName} OK!", LogType.InfoLog);
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
            this._viewLogger.WriteLog("PROCESSED A DEVICE SELECTION CHANGED EVENT!", LogType.InfoLog);

            // Convert sender and cast our DLL object
            ListBox SendingBox = (ListBox)SendingDeviceObject;
            string SelectedDevice = SendingBox.SelectedItem?.ToString();

            // BUG: THIS LOG ENTRY HANGS THE WHOLE APP?
            this._viewLogger.WriteLog(
                string.IsNullOrWhiteSpace(SelectedDevice) ? "NO DLL ENTRY SELECTED! CLEARING" : $"DEVICE ENTRY PULLED: {SelectedDevice}",
                LogType.TraceLog
            );

            // Log and populate devices
            this.ViewModel.SelectedDevice = SelectedDevice;
            if (SelectedDevice != null) this._viewLogger.WriteLog($"POPULATED OUR DEVICE ENTRY NAMED {SelectedDevice} ON OUR VIEW MODEL OK!", LogType.InfoLog);
        }
    }
}
