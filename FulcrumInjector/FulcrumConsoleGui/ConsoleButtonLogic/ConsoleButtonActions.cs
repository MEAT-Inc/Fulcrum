using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumConsoleGui.ConsoleButtonLogic
{
    /// <summary>
    /// Static methods used by the console GUI for menu controls.
    /// </summary>
    public static class ConsoleButtonActions
    {
        /// <summary>
        /// Exits this application using the environment exit method.
        /// </summary>
        /// <param name="ExitCode">Defined exit code to use.</param>
        public static void ConsoleAppExit(int ExitCode = 0)
        {
            // Built a logger object for this action
            var ExitLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("ExitLogger")) ?? new SubServiceLogger("ExitLogger");

            // Log exiting and quit.
            ExitLogger.WriteLog($"EXITING APPLICATION NOW WITH EXIT CODE {ExitLogger}", LogType.WarnLog);
            Environment.Exit(ExitCode);
        }

        #region Application Popups
        /// <summary>
        /// Shows a popup of all the log files for this, the fulcrum, and other logs made during execution
        /// </summary>
        public static void ShowLogFilesPopup()
        {
            // Built a logger object for this action
            var LogFilesPopupLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("LogFilesPopupLogger")) ?? new SubServiceLogger("LogFilesPopupLogger");

            // Now show the popup object here for the help window
            LogFilesPopupLogger.WriteLog("SHOWING ALL FULCRUM LOG FILE OBJECTS NOW...");
        }
        /// <summary>
        /// Shows the info/help display popup for this application
        /// </summary>
        public static void ShowHelpPopup()
        {
            // Built a logger object for this action
            var HelpPopupLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("HelpPopupLogger")) ?? new SubServiceLogger("HelpPopupLogger");

            // Now show the popup object here for the help window
            HelpPopupLogger.WriteLog("SHOWING HELP AND DIAGNOSTIC INFORMATION NOW...");
        }
        /// <summary>
        /// Shows a popup with version information about this application, the DLL for the fulcrum, and other info
        /// </summary>
        public static void ShowVersionPopup()
        {
            // Built a logger object for this action
            var VersionPopupLogger = (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
                .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("VersionPopupLogger")) ?? new SubServiceLogger("VersionPopupLogger");

            // Now show the popup object here with version information
            VersionPopupLogger.WriteLog("SHOWING VERSION INFORMATION NOW...");
        }
        #endregion
    }
}
