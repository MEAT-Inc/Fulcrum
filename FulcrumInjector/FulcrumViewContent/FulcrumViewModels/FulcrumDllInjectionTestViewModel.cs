using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using SharpLogging;
using SharpSupport;

namespace FulcrumInjector.FulcrumViewContent.FulcrumViewModels
{
    /// <summary>
    /// View Model for Injection Test View
    /// </summary>
    internal class FulcrumDllInjectionTestViewModel : FulcrumViewModelBase
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing fields for our public properties
        private bool _injectionLoadPassed;      // Pass or fail for our injection load process
        private string _injectorDllPath;        // Private value for title view title text
        private string _injectorTestResult;     // Private value for title view version text

        #endregion //Fields

        #region Properties

        // Public properties for the view to bind onto  
        public string InjectorDllPath { get => _injectorDllPath; set => PropertyUpdated(value); }
        public string InjectorTestResult { get => _injectorTestResult; set => PropertyUpdated(value); }
        public bool InjectionLoadPassed { get => _injectionLoadPassed; set => PropertyUpdated(value); }

        #endregion //Properties

        #region Structs and Classes

        // PT Open Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruOpen(string DeviceName, out ulong DeviceId);
        public DelegatePassThruOpen PTOpen;

        // PT Open Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruClose(uint DeviceId);
        public DelegatePassThruClose PTClose;

        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        /// <param name="DllInjectionView">UserControl which holds the content for our DLL Testing view</param>
        public FulcrumDllInjectionTestViewModel(UserControl DllInjectionView) : base(DllInjectionView)
        {
            // Spawn a new logger for this view model instance 
            this.ViewModelLogger = new SharpLogger(LoggerActions.UniversalLogger);

            // Log information and store values 
            this.ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store title and version string values now.
            this.InjectorTestResult = "Not Yet Tested";
            this.InjectorDllPath =
#if DEBUG
               Path.GetFullPath("..\\..\\..\\FulcrumShim\\Debug\\FulcrumShim.dll");
#else
                ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorDllInformation.FulcrumDLL");  
#endif

            // Log information about the DLL Path values
            this.ViewModelLogger.WriteLog("LOCATED NEW DLL PATH VALUE OK!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog($"DLL PATH VALUE PULLED: {this.InjectorDllPath}");
            this.ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION TESTER VALUES OK!", LogType.InfoLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test the loading process of the fulcrum DLL Injection objects
        /// </summary>
        /// <param name="ResultString">Result String of the injection</param>
        /// <returns>True if the DLL Injects OK. False if not.</returns>
        public bool TestInjectorDllLoading(out string ResultString)
        {
            // Make sure we need to be rerunning this.
            if (this.InjectionLoadPassed || this.InjectorTestResult == "Injection Passed!")
            {
                // Log info and build return values
                this.ViewModelLogger.WriteLog("PREVIOUS TEST WAS SUCCESSFUL! RETURNING VALUES ACCORDINGLY NOW...", LogType.InfoLog);
                FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.IsEnabled = false;
                FulcrumConstants.FulcrumDllInjectionTestView.TestInjectionButton.ToolTip = "To retry injection, please restart this application";

                // Return output values
                ResultString = this.InjectorTestResult;
                return this.InjectionLoadPassed;
            }

            // Begin by loading the DLL Object
            this.InjectorTestResult = "Testing...";
            this.ViewModelLogger.WriteLog($"PULLING IN FULCRUM DLL NOW", LogType.InfoLog);
            IntPtr LoadResult = Win32Invokers.LoadLibrary(this.InjectorDllPath);
            this.ViewModelLogger.WriteLog($"RESULT FROM LOADING DLL: {LoadResult}", LogType.InfoLog);

            // Make sure the pointer is not 0s. 
            if (LoadResult == IntPtr.Zero)
            {
                // Log failure, set output value and return false
                var ErrorCode = Win32Invokers.GetLastError();
                this.ViewModelLogger.WriteLog("FAILED TO LOAD OUR NEW DLL INSTANCE FOR OUR APPLICATION!", LogType.ErrorLog);
                this.ViewModelLogger.WriteLog($"ERROR CODE PROCESSED FROM LOADING REQUEST WAS: {ErrorCode}", LogType.ErrorLog);

                // Store failure message output
                this.InjectorTestResult = $"Failed! IntPtr.Zero! ({ErrorCode})";
                ResultString = this.InjectorTestResult;
                return false;
            }

            // Log Passed and then unload our DLL
            this.ViewModelLogger.WriteLog($"DLL LOADING WAS SUCCESSFUL! POINTER ASSIGNED: {LoadResult}", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("UNLOADING DLL FOR USE BY THE OE APPS LATER ON...");

            // Log information and run the injector test
            this.ViewModelLogger.WriteLog("RUNNING INJECTION TEST NOW...", LogType.WarnLog);
            if (this.TestInjectorDllSelectionBox(LoadResult, out ResultString)) {
                this.ViewModelLogger.WriteLog($"RESULT FROM INJECTION: {ResultString}", LogType.InfoLog);
            }
            else
            {
                // Log injection via selection box failed and then setup a new call
                this.ViewModelLogger.WriteLog("FAILED TO INJECT USING SELECTION BOX!", LogType.ErrorLog);
                return false;
            }

            // Run our unload calls here
            if (!Win32Invokers.FreeLibrary(LoadResult))
            {
                // Get Error code and build message
                var ErrorCode = Win32Invokers.GetLastError();
                this.InjectorTestResult = $"Unload Error! ({ErrorCode})";
                ResultString = this.InjectorTestResult;

                // Write log output
                this.ViewModelLogger.WriteLog("FAILED TO UNLOAD DLL! THIS IS FATAL!", LogType.ErrorLog);
                this.ViewModelLogger.WriteLog($"ERROR CODE PROCESSED FROM UNLOADING REQUEST WAS: {ErrorCode}", LogType.ErrorLog);
                return false;
            }

            // Return passed and set results.
            this.ViewModelLogger.WriteLog("UNLOADED DLL OK!", LogType.InfoLog);
            this.InjectorTestResult = "Injection Passed!";
            ResultString = this.InjectorTestResult;

            // Log information output
            this.ViewModelLogger.WriteLog("----------------------------------------------", LogType.WarnLog);
            this.ViewModelLogger.WriteLog("IMPORT PROCESS SHOULD NOT HAVE ISSUES GOING FORWARD!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("THIS MEANS THE FULCRUM APP SHOULD WORK AS EXPECTED!", LogType.InfoLog);
            this.ViewModelLogger.WriteLog("----------------------------------------------", LogType.WarnLog);
            return true;
        }
        /// <summary>
        /// Tests the use of the injector selection box when new instances are made
        /// </summary>
        public bool TestInjectorDllSelectionBox(IntPtr InjectorDllPtr, out string ResultString)
        {
            try
            {
                // Import our PTOpen command
                this.ViewModelLogger.WriteLog("IMPORTING PT OPEN METHOD AND NOW...", LogType.WarnLog);
                IntPtr PassThruOpenCommand = Win32Invokers.GetProcAddress(InjectorDllPtr, "PassThruOpen");
                PTOpen = (DelegatePassThruOpen)Marshal.GetDelegateForFunctionPointer(PassThruOpenCommand, typeof(DelegatePassThruOpen));
                this.ViewModelLogger.WriteLog("IMPORTED PTOPEN METHOD OK!", LogType.InfoLog);

                // Import our PTClose command
                this.ViewModelLogger.WriteLog("IMPORTING PT CLOSE METHOD NOW...", LogType.WarnLog);
                IntPtr PassThruCloseCommand = Win32Invokers.GetProcAddress(InjectorDllPtr, "PassThruClose");
                PTClose = (DelegatePassThruClose)Marshal.GetDelegateForFunctionPointer(PassThruCloseCommand, typeof(DelegatePassThruClose));
                this.ViewModelLogger.WriteLog("IMPORTED PTCLOSE METHOD OK!", LogType.InfoLog);

                // Log importing passed so far
                this.ViewModelLogger.WriteLog("IMPORTED PT OPEN AND CLOSE COMMANDS WITHOUT ISSUES!", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"PTOPEN POINTER: {PassThruOpenCommand}", LogType.TraceLog);
                this.ViewModelLogger.WriteLog($"PTCLOSE POINTER: {PassThruCloseCommand}", LogType.TraceLog);
            }
            catch (Exception ImportEx)
            {
                // Log failed to connect to our pipe.
                this.ViewModelLogger.WriteLog($"FAILED TO IMPORT A PASSTHRU METHOD USING OUR INJECTED DLL!", LogType.ErrorLog);
                this.ViewModelLogger.WriteLog("EXCEPTION THROWN DURING DYNAMIC CALL OF THE UNMANAGED PT COMMAND!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN", ImportEx);

                // Store output values and fail
                ResultString = "DLL Import Failed!";
                return false;
            }

            try
            {
                // Invoke PTOpen now then run our PT Close method
                PTOpen.Invoke(null, out ulong DeviceId);
                this.ViewModelLogger.WriteLog("INVOKE METHOD PASSED! OUTPUT IS BEING LOGGED CORRECTLY AND ALL SELECTION BOX ENTRIES NEEDED ARE POPULATING NOW", LogType.InfoLog);
                this.ViewModelLogger.WriteLog($"DEVICE ID RETURNED: {DeviceId}");

                // Now issue the close command
                this.ViewModelLogger.WriteLog("TRYING TO CLOSE OPENED DEVICE INSTANCE NOW...", LogType.WarnLog);
                PTClose.Invoke((uint)DeviceId);

                // Set output, return values.
                ResultString = "DLL Execution Passed!";
                this.ViewModelLogger.WriteLog("INJECTED METHOD EXECUTION COMPLETED OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception ImportEx)
            {
                // Log failed to connect to our pipe.
                this.ViewModelLogger.WriteLog($"FAILED TO EXECUTE A PASSTHRU METHOD USING OUR INJECTED DLL!", LogType.ErrorLog);
                this.ViewModelLogger.WriteLog("EXCEPTION THROWN DURING DYNAMIC CALL OF THE UNMANAGED PT COMMAND!", LogType.ErrorLog);
                this.ViewModelLogger.WriteException("EXCEPTION THROWN", ImportEx);

                // Store output values and fail
                ResultString = "DLL Execution Failed!";
                return false;
            }
        }
    }
}
