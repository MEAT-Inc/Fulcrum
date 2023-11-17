using SharpLogging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FulcrumService;
using FulcrumSupport;
using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Collections;
using FulcrumEncryption;

namespace FulcrumInjector.FulcrumViewContent
{
    /// <summary>
    /// Interaction logic for FulcrumServiceErrorWindow.xaml
    /// </summary>
    public partial class FulcrumServiceErrorWindow : MetroWindow, INotifyPropertyChanged, INotifyCollectionChanged
    {
        #region Custom Events

        // Public facing event for hooking into property or collection changed calls
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Event handler for when a property is updated on this view model controller
        /// </summary>
        /// <param name="PropertyName">Name of the property being updated</param>
        private void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            // Invoke the event for our property if the event handler is configured
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
        /// <summary>
        /// Event handler for when a collection is updated on this view model controller
        /// </summary>
        /// <param name="CollectionAction">The action for the collection changed or updated</param>
        /// <param name="CollectionChanged">The collection which is being updated</param>
        private void OnCollectionChanged(NotifyCollectionChangedAction CollectionAction, IList CollectionChanged)
        {
            // Invoke the event for our property if the event handler is configured
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(CollectionAction, CollectionChanged));
        }     

        /// <summary>
                 /// Private helper method used to set the backing field value for a property when updated
                 /// </summary>
                 /// <typeparam name="T">The type of the property being updated</typeparam>
                 /// <param name="Field">The field to update</param>
                 /// <param name="FieldValue">The value of the field to store</param>
                 /// <param name="PropertyName">The name of the property being updated</param>
                 /// <returns>True if the property value is changed. False if not</returns>
        private bool SetField<T>(ref T Field, T FieldValue, [CallerMemberName] string PropertyName = null)
        {
            // Check if our field value needs to be updated or not
            if (EqualityComparer<T>.Default.Equals(Field, FieldValue)) return false;

            // Update the field, fire a property changed event and move on 
            Field = FieldValue;
            OnPropertyChanged(PropertyName);
            return true;
        }

        #endregion // Custom Events

        #region Fields

        // Logger instance for this view content
        private readonly SharpLogger _viewLogger;

        // Private backing fields for public facing properties 
        private string _serviceInstallPath;                                         // Path to the injector app services folder
        private ObservableCollection<FulcrumServiceBase.FulcrumServiceInfo> _serviceInformation;       // Services found or not found on the system

        #endregion // Fields

        #region Properties

        // Public facing properties holding information about our install paths
        public string ServiceInstallPath
        {
            get => this._serviceInstallPath;
            set => this.SetField(ref this._serviceInstallPath, value);
        }
        public ObservableCollection<FulcrumServiceBase.FulcrumServiceInfo> ServiceInformation
        {
            get => this._serviceInformation;
            set => this.SetField(ref this._serviceInformation, value);
        }
        public bool ServicesConfigured => this.ServiceInformation.All(ServiceInfo => ServiceInfo.ServiceInstalled);

        #endregion // Properties

        #region Structs and Classes
        #endregion // Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new view object instance for our service error status window
        /// </summary>
        /// <param name="ServiceInformation">Service status information for all services on the system</param>
        private FulcrumServiceErrorWindow()
        {
            // Initialize new UI Component and configure the logger instance for it
            this._viewLogger = new SharpLogger(LoggerActions.UniversalLogger);
            InitializeComponent();

            // Find our service states before building a new window
            this._viewLogger.WriteLog("RELOADING SERVICE STATES NOW....", LogType.WarnLog);
            var ServiceStates = FulcrumServiceBase.GetServiceStates();

            // Store our service base install path value here 
            this.ServiceInstallPath = RegistryControl.InjectorServiceInstallPath;
            this._viewLogger.WriteLog($"STORED SERVICE INSTALL PATH OF {this.ServiceInstallPath}!", LogType.InfoLog);

            // Configure a new collection to store our service states on here
            this.ServiceInformation = new ObservableCollection<FulcrumServiceBase.FulcrumServiceInfo>();
            this._viewLogger.WriteLog("BUILT NEW COLLECTION FOR SERVICE STATE INFORMATION! POPULATING NOW...", LogType.InfoLog);

            // Iterate all of our service information objects here and store them
            foreach (var ServiceState in ServiceStates)
            {
                // Add our service state and log it out here 
                this.ServiceInformation.Add(ServiceState);
                this._viewLogger.WriteLog($"ADDED SERVICE STATE INFORMATION FOR SERVICE {ServiceState.ServiceName}!", LogType.InfoLog);
            }

            // Log out that we've built service install information and continue on
            this._viewLogger.WriteLog("BUILT INSTALL INFORMATION FOR ALL SERVICE INSTANCES CORRECTLY!", LogType.InfoLog);
            this._viewLogger.WriteLog("SHOWING SERVICE INFORMATION TO USER NOW...", LogType.InfoLog);
        }
        /// <summary>
        /// Static helper method used to invoke a new service status window to show the user service issues
        /// </summary>
        /// <returns>True if all services are built correctly. False if one or more services are missing</returns>
        public static bool ValidateServiceConfiguration()
        {
            // Build a new service configuration window and check our state
            FulcrumServiceErrorWindow ErrorWindow = new FulcrumServiceErrorWindow();
            if (ErrorWindow.ServicesConfigured) 
            {
                // Log out that we've found all valid services and exit out passed
                ErrorWindow._viewLogger.WriteLog("ALL SERVICES ARE CONFIGURED CORRECTLY!" ,LogType.InfoLog);
                ErrorWindow._viewLogger.WriteLog("NO NEED TO DISPLAY SERVICE INFORMATION TO THE USER AT THIS TIME!", LogType.InfoLog);
                return true;
            }

            // Show the window and return out false once done.
            ErrorWindow.ShowDialog();
            return false;
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Event handler used to process a close injector request when the user is done viewing this window
        /// </summary>
        /// <param name="Sender">The sending button that fired this event</param>
        /// <param name="E">EventArgs fired along with the click event</param>
        private void btnCloseInjectorApplication_OnClick(object Sender, RoutedEventArgs E)
        {
            // If keys are not configured, close the window and log out this result 
            this._viewLogger.WriteLog("INJECTOR SERVICE STATUS HAS BEEN SHOWN AND IS BEING REQUESTED TO CLOSE!", LogType.WarnLog);
            this._viewLogger.WriteLog("INJECTOR APP CAN NOT PROCEED WITHOUT VALID SERVICE CONFIGURATION! KILLING NOW...", LogType.WarnLog);
            this.Close();
        }
    }
}
