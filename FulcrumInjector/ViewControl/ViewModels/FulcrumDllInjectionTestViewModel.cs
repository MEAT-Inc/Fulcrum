using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.JsonHelpers;
using FulcrumInjector.ViewControl.Models;
using FulcrumInjector.ViewControl.Views;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.ViewModels
{
    public class FulcrumDllInjectionTestViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorTestViewModelLogger")) ?? new SubServiceLogger("InjectorTestViewModelLogger");

        // Private control values
        private bool _injectionLoadPassed;      // Pass or fail for our injection load process
        private string _injectorDllPath;        // Private value for title view title text
        private string _injectorTestResult;     // Private value for title view version text

        // Title string and the title view version bound values
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
            ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store title and version string values now.
            this.InjectorDllPath = ValueLoaders.GetConfigValue<string>("FulcrumInjectorSettings.FulcrumDLL");
            this.InjectorTestResult = "Not Yet Tested";
            ViewModelLogger.WriteLog("LOCATED NEW DLL PATH VALUE OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"DLL PATH VALUE PULLED: {this.InjectorDllPath}");
            
            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW DLL INJECTION TESTER VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Writes text to our output logging box for the debugging of the injection process
        /// </summary>
        /// <param name="LogText"></param>
        internal void WriteToLogBox(string LogText)
        {
            // Build the current View object into our output and then log into it.
            FulcrumDllInjectionTestView TestView = this.BaseViewControl as FulcrumDllInjectionTestView;

            // Now append text
            TestView.InjectorTestOutput.Text += LogText.Trim() + "\n";
            ViewModelLogger.WriteLog($"[DEBUG OUTPUT BOX] ::: {LogText}", LogType.TraceLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Test the loading process of the fulcrum DLL Injection objects
        /// </summary>
        /// <param name="InjectionResult">Result String of the injection</param>
        /// <returns>True if the DLL Injects OK. False if not.</returns>
        internal bool PerformDllInjectionTest(out string ResultString)
        {
            // Begin by loading the DLL Object
            this.InjectorTestResult = "Testing...";
            WriteToLogBox($"PULLING IN FULCRUM DLL NOW");
            IntPtr LoadResult = FulcrumDllInjectionTestModel.LoadLibrary(this.InjectorDllPath);
            WriteToLogBox($"RESULT FROM LOADING DLL: {LoadResult}");

            // Make sure the pointer is not 0s. 
            if (LoadResult == IntPtr.Zero)
            {
                // Log failure, set output value and return false
                var ErrorCode = FulcrumDllInjectionTestModel.GetLastError();
                WriteToLogBox("FAILED TO LOAD OUR NEW DLL INSTANCE FOR OUR APPLICATION!");
                WriteToLogBox($"ERROR CODE PROCESSED FROM LOADING REQUEST WAS: {ErrorCode}");

                // Store failure message output
                this.InjectorTestResult = $"Failed! IntPtr.Zero! ({ErrorCode})";
                ResultString = this.InjectorTestResult;
                return false;
            }

            // Log Passed and then unload our DLL
            WriteToLogBox($"DLL LOADING WAS SUCCESSFUL! POINTER ASSIGNED: {LoadResult}");
            WriteToLogBox("UNLOADING DLL FOR USE BY THE OE APPS LATER ON...");
            if (!FulcrumDllInjectionTestModel.FreeLibrary(LoadResult))
            {
                // Get Error code and build message
                var ErrorCode = FulcrumDllInjectionTestModel.GetLastError();
                this.InjectorTestResult = $"Unload Error! ({ErrorCode})";
                ResultString = this.InjectorTestResult;

                // Write log output
                WriteToLogBox("FAILED TO UNLOAD DLL! THIS IS FATAL!");
                WriteToLogBox($"ERROR CODE PROCESSED FROM UNLOADING REQUEST WAS: {ErrorCode}");
                return false;
            }

            // Return passed and set results.
            WriteToLogBox("UNLOADED DLL OK!");
            this.InjectorTestResult = "Injection Passed!";
            ResultString = this.InjectorTestResult;

            // Log information output
            WriteToLogBox("----------------------------------------------");
            WriteToLogBox("IMPORT PROCESS SHOULD NOT HAVE ISSUES GOING FORWARD!");
            WriteToLogBox("THIS MEANS THE FULCRUM APP SHOULD WORK AS EXPECTED!");
            WriteToLogBox("----------------------------------------------");
            return true;
        }
    }
}
