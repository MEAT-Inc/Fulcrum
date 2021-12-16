using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl.Models;
using FulcrumInjector.ViewControl.Views;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.ViewModels
{
    /// <summary>
    /// View Model for Injection Test View
    /// </summary>
    public class FulcrumDllInjectionTestViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorTestViewModelLogger")) ?? new SubServiceLogger("InjectorTestViewModelLogger");

        // Private control values
        private bool _injectionLoadPassed;      // Pass or fail for our injection load process
        private string _injectorDllPath;        // Private value for title view title text
        private string _injectorTestResult;     // Private value for title view version text

        // Public values for our view to bind onto 
        public string InjectorDllPath { get => _injectorDllPath; set => PropertyUpdated(value); }
        public string InjectorTestResult { get => _injectorTestResult; set => PropertyUpdated(value); }
        public bool InjectionLoadPassed { get => _injectionLoadPassed; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumDllInjectionTestViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP INJECTOR TEST VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store title and version string values now.
            this.InjectorDllPath = ValueLoaders.GetConfigValue<string>("FulcrumInjectorSettings.FulcrumDLL");
            this.InjectorTestResult = "Not Yet Tested";
            ViewModelLogger.WriteLog("LOCATED NEW DLL PATH VALUE OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"DLL PATH VALUE PULLED: {this.InjectorDllPath}");
            
            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION TESTER VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        // PT Open Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruOpen(IntPtr DllPointer, out uint DeviceId);
        public DelegatePassThruOpen PTOpen;

        // PT Open Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePassThruClose(uint DeviceId);
        public DelegatePassThruClose PTClose;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test the loading process of the fulcrum DLL Injection objects
        /// </summary>
        /// <param name="InjectionResult">Result String of the injection</param>
        /// <returns>True if the DLL Injects OK. False if not.</returns>
        internal bool TestInjectorDllLoading(out string ResultString, bool SkipSelectionBox = false)
        {
            // Begin by loading the DLL Object
            this.InjectorTestResult = "Testing...";
            ViewModelLogger.WriteLog($"PULLING IN FULCRUM DLL NOW", LogType.InfoLog);
            IntPtr LoadResult = FulcrumWin32Invokers.LoadLibrary(this.InjectorDllPath);
            ViewModelLogger.WriteLog($"RESULT FROM LOADING DLL: {LoadResult}", LogType.InfoLog);

            // Make sure the pointer is not 0s. 
            if (LoadResult == IntPtr.Zero)
            {
                // Log failure, set output value and return false
                var ErrorCode = FulcrumWin32Invokers.GetLastError();
                ViewModelLogger.WriteLog("FAILED TO LOAD OUR NEW DLL INSTANCE FOR OUR APPLICATION!", LogType.ErrorLog);
                ViewModelLogger.WriteLog($"ERROR CODE PROCESSED FROM LOADING REQUEST WAS: {ErrorCode}", LogType.ErrorLog);

                // Store failure message output
                this.InjectorTestResult = $"Failed! IntPtr.Zero! ({ErrorCode})";
                ResultString = this.InjectorTestResult;
                return false;
            }

            // Log Passed and then unload our DLL
            ViewModelLogger.WriteLog($"DLL LOADING WAS SUCCESSFUL! POINTER ASSIGNED: {LoadResult}", LogType.InfoLog);
            ViewModelLogger.WriteLog("UNLOADING DLL FOR USE BY THE OE APPS LATER ON...");
            
            // If Pipes are open, don't try test injection methods
            if (!SkipSelectionBox) { this.TestInjectorDllSelectionBox(LoadResult, out ResultString); }
            else { ViewModelLogger.WriteLog("PIPES ARE SEEN TO BE OPEN! NOT TESTING INJECTION SELECTION BOX ROUTINE!", LogType.WarnLog); }

            // Run our unload calls here
            if (!FulcrumWin32Invokers.FreeLibrary(LoadResult))
            {
                // Get Error code and build message
                var ErrorCode = FulcrumWin32Invokers.GetLastError();
                this.InjectorTestResult = $"Unload Error! ({ErrorCode})";
                ResultString = this.InjectorTestResult;

                // Write log output
                ViewModelLogger.WriteLog("FAILED TO UNLOAD DLL! THIS IS FATAL!", LogType.ErrorLog);
                ViewModelLogger.WriteLog($"ERROR CODE PROCESSED FROM UNLOADING REQUEST WAS: {ErrorCode}", LogType.ErrorLog);
                return false;
            }

            // Return passed and set results.
            ViewModelLogger.WriteLog("UNLOADED DLL OK!", LogType.InfoLog);
            this.InjectorTestResult = "Injection Passed!";
            ResultString = this.InjectorTestResult;

            // Log information output
            ViewModelLogger.WriteLog("----------------------------------------------", LogType.WarnLog);
            ViewModelLogger.WriteLog("IMPORT PROCESS SHOULD NOT HAVE ISSUES GOING FORWARD!", LogType.InfoLog);
            ViewModelLogger.WriteLog("THIS MEANS THE FULCRUM APP SHOULD WORK AS EXPECTED!", LogType.InfoLog);
            ViewModelLogger.WriteLog("----------------------------------------------", LogType.WarnLog);
            return true;
        }

        /// <summary>
        /// Tests the use of the injector selection box when new instances are made
        /// </summary>
        internal bool TestInjectorDllSelectionBox(IntPtr InjectorDllPtr, out string ResultString)
        {
            try
            {
                // Import our PTOpen command
                ViewModelLogger.WriteLog("IMPORTING PT OPEN METHOD AND NOW...", LogType.WarnLog);
                IntPtr PassThruOpenCommand = FulcrumWin32Invokers.GetProcAddress(InjectorDllPtr, "PassThruOpen");
                PTOpen = (DelegatePassThruOpen)Marshal.GetDelegateForFunctionPointer(PassThruOpenCommand, typeof(DelegatePassThruOpen));
                ViewModelLogger.WriteLog("IMPORTED PTOPEN METHOD OK!", LogType.InfoLog);

                // Import our PTClose command
                ViewModelLogger.WriteLog("IMPORTING PT CLOSE METHOD NOW...", LogType.WarnLog);
                IntPtr PassThruCloseCommand = FulcrumWin32Invokers.GetProcAddress(InjectorDllPtr, "PassThruClose");
                PTClose = (DelegatePassThruClose)Marshal.GetDelegateForFunctionPointer(PassThruCloseCommand, typeof(DelegatePassThruClose));
                ViewModelLogger.WriteLog("IMPORTED PTCLOSE METHOD OK!", LogType.InfoLog);

                // Log importing passed so far
                ViewModelLogger.WriteLog("IMPORTED PT OPEN AND CLOSE COMMANDS WITHOUT ISSUES!", LogType.InfoLog);
                ViewModelLogger.WriteLog($"PTOPEN POINTER: {PassThruOpenCommand}", LogType.TraceLog);
                ViewModelLogger.WriteLog($"PTCLOSE POINTER: {PassThruCloseCommand}", LogType.TraceLog);
            }
            catch (Exception ImportEx)
            {
                // Log failed to connect to our pipe.
                ViewModelLogger.WriteLog($"FAILED TO IMPORT A PASSTHRU METHOD USING OUR INJECTED DLL!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN DURING DYNAMIC CALL OF THE UNMANAGED PT COMMAND!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN", ImportEx);

                // Store output values and fail
                ResultString = "DLL Import Failed!";
                return false;
            }

            try
            {
                // Invoke PTOpen now then run our PT Close method
                PTOpen.Invoke(InjectorDllPtr, out uint DeviceId);
                ViewModelLogger.WriteLog("INVOKE METHOD PASSED! OUTPUT IS BEING LOGGED CORRECTLY AND ALL SELECTION BOX ENTRIES NEEDED ARE POPULATING NOW", LogType.InfoLog);
                ViewModelLogger.WriteLog($"DEVICE ID RETURNED: {DeviceId}");

                // Now issue the close command
                ViewModelLogger.WriteLog("TRYING TO CLOSE OPENED DEVICE INSTANCE NOW...", LogType.WarnLog);
                PTClose.Invoke(DeviceId);
                ViewModelLogger.WriteLog("INJECTED METHOD EXECUTION COMPLETED OK!", LogType.InfoLog);

                // Set output, return values.
                ResultString = "DLL Execution Passed!";
                return true;
            }
            catch (Exception ImportEx)
            {
                // Log failed to connect to our pipe.
                ViewModelLogger.WriteLog($"FAILED TO EXECUTE A PASSTHRU METHOD USING OUR INJECTED DLL!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN DURING DYNAMIC CALL OF THE UNMANAGED PT COMMAND!", LogType.ErrorLog);
                ViewModelLogger.WriteLog("EXCEPTION THROWN", ImportEx);

                // Store output values and fail
                ResultString = "DLL Execution Failed!";
                return false;
            }
        }
    }
}
