using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using FulcrumInjector.FulcrumLogic.ExtensionClasses;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic
{
    /// <summary>
    /// Log heading type values
    /// </summary>
    public enum EntryHeadingType
    {
        // Types of log entry heading values
        [Description("")] NEWLINE,
        [Description("      ")] NONE,
        [Description("------")] DEBUG,
        [Description("++++++")] CREATED,
        [Description("!!!!!!")] FAILURE
    }

    /// <summary>
    /// Static calls to our fulcrum DLL for logging output
    /// </summary>
    public static class FulcrumWin32Logging
    {
        // Logger object.
        private static SubServiceLogger ExportLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorLogExportLogger")) ?? new SubServiceLogger("InjectorLogExportLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // DLL Path value configuration
        private static readonly string InjectorDllPath =
#if DEBUG
        "..\\..\\..\\FulcrumShim\\Debug\\FulcrumShim.dll";
#else
                ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.InjectorDllInformation.FulcrumDLL");  
#endif

        // --------------------------------------------------------------------------------------------------------------------------

        // PT Write Log Append Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePTWriteLogA([MarshalAs(UnmanagedType.LPStr)] string MessageValue);
        private static DelegatePTWriteLogA PTWriteLogA;

        // PT Write Log Write Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePTWriteLogW([MarshalAs(UnmanagedType.LPWStr)] string MessageValue);
        private static DelegatePTWriteLogW PTWriteLogW;
      
        // PT Save Log Method object
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int DelegatePTSaveLog([MarshalAs(UnmanagedType.LPWStr)] string LogFilePath);
        private static DelegatePTSaveLog PTSaveLog;

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Confirms the pointers for the writing methods are built
        /// </summary>
        /// <returns>True if built and passed. False if not</returns>
        private static bool ValidateFunctionRefs()
        {
            // Check if not null.
            if (PTWriteLogA != null && PTWriteLogW != null) return true;

            // Import the DLL object first
            IntPtr LoadResult = FulcrumWin32Setup.LoadLibrary(InjectorDllPath);
            if (LoadResult == IntPtr.Zero) {
                ExportLogger.WriteLog("FAILED TO IMPORT OUR DLL INSTANCE!", LogType.ErrorLog);
                return false;
            }

            // Try mapping our values out here. Return true if passed. False if not
            try
            {
                // Import our PTWriteA command
                ExportLogger.WriteLog("IMPORTING PTWRITELOG - A METHOD AND NOW...", LogType.WarnLog);
                IntPtr PassThruWriteACommand = FulcrumWin32Setup.GetProcAddress(LoadResult, "PassThruWriteToLogA");
                PTWriteLogA = (DelegatePTWriteLogA)Marshal.GetDelegateForFunctionPointer(PassThruWriteACommand, typeof(DelegatePTWriteLogA));
                ExportLogger.WriteLog("IMPORTED PTWRITELOG - A METHOD OK!", LogType.InfoLog);

                // Import our PTWriteW command
                ExportLogger.WriteLog("IMPORTING PTWRITELOG - W METHOD NOW...", LogType.WarnLog);
                IntPtr PassThruWriteWCommand = FulcrumWin32Setup.GetProcAddress(LoadResult, "PassThruWriteToLogW");
                PTWriteLogW = (DelegatePTWriteLogW)Marshal.GetDelegateForFunctionPointer(PassThruWriteWCommand, typeof(DelegatePTWriteLogW));
                ExportLogger.WriteLog("IMPORTED PTWRITELOG - W METHOD OK!", LogType.InfoLog);

                // Import our PTSave command
                ExportLogger.WriteLog("IMPORTING PTSAVE METHOD NOW...", LogType.WarnLog);
                IntPtr PassThruSaveCommand = FulcrumWin32Setup.GetProcAddress(LoadResult, "PassThruSaveLog");
                PTSaveLog = (DelegatePTSaveLog)Marshal.GetDelegateForFunctionPointer(PassThruSaveCommand, typeof(DelegatePTSaveLog));
                ExportLogger.WriteLog("IMPORTED PTSAVE METHOD OK!", LogType.InfoLog);

                // Return passed
                return true;
            }
            catch { return false; }
        }

        // ----------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Saves a log file buffer to a given file path
        /// </summary>
        /// <param name="LogFilePath">Path to save log file into</param>
        /// <returns>True if saved. False if not</returns>
        public static bool PassThruSaveLog(string LogFilePath)
        {
            // Validate method state and run it.
            if (!ValidateFunctionRefs()) return false;
            try
            {
                // Return true for called method ok
                PTSaveLog.Invoke(LogFilePath);
                return true;
            }
            catch
            {
                // Return false for failed invoke call
                return false;
            }
        }
        /// <summary>
        /// Logs a new message value using the PTWriteLogA call
        /// </summary>
        /// <param name="MessageToLog"></param>
        /// <returns></returns>
        public static bool PassThruWriteLog_A(EntryHeadingType Header, string MessageToLog)
        {
            // Validate method state and run it.
            if (!ValidateFunctionRefs()) return false;
            try
            {
                // Return true for called method ok
                if (Header == EntryHeadingType.NEWLINE) PTWriteLogA(MessageToLog); 
                else PTWriteLogA.Invoke($"{Header.ToDescriptionString()}{MessageToLog}");
                return true;
            }
            catch
            {
                // Return false for failed invoke call
                return false;
            }
        }
        /// <summary>
        /// Logs a new message value using the PTWriteLogA call
        /// </summary>
        /// <param name="MessageToLog"></param>
        /// <returns></returns>
        public static bool PassThruWriteLog_W(EntryHeadingType Header, string MessageToLog)
        {
            // Validate method state and run it.
            if (!ValidateFunctionRefs()) return false;
            try
            {
                // Return true for called method ok
                if (Header == EntryHeadingType.NEWLINE) PTWriteLogW(MessageToLog);
                PTWriteLogW.Invoke($"{Header.ToDescriptionString()}{MessageToLog}");
                return true;
            }
            catch
            {
                // Return false for failed invoke call
                return false;
            }
        }
    }
}
