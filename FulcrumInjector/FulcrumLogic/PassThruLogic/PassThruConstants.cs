using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using SharpWrap2534;
using SharpWrap2534.SupportingLogic;

namespace FulcrumInjector.FulcrumLogic.PassThruLogic
{
    /// <summary>
    /// Enum used to configure the session we want to control
    /// </summary>
    public enum SharpSession
    {
        [Description("FulcrumInjector PassThru")]   SessionAlpha,       // Session Alpha for the AutoID routines
        [Description("FulcrumInjector Simulation")] SessionBravo,       // Session Bravo for simulation running
    }
    /// <summary>
    /// Struct for setting up sessions so we don't have to pass in params all the time.
    /// </summary>
    public struct SessionInit
    {
        // Version of the J2534 DLL In use
        public JVersion Version;

        // Device And DLL
        public string DLLName;
        public string DeviceName;
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Class containing our instances for our SharpSessions
    /// </summary>
    public static class PassThruConstants
    {
        // Logger object.
        private static SubServiceLogger ConstantsLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("PassThruConstantsLogger")) ?? new SubServiceLogger("PassThruConstantsLogger");

        // SharpWrap Sessions. Two of them total. One for the AutoID routines, one for the Simulation running
        public static Sharp2534Session SharpSessionAlpha;
        public static Sharp2534Session SharpSessionBravo;

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures a new SharpSession instance for our given session type and params passed
        /// </summary>
        /// <param name="SessionType">Session to open. Simulation or not.</param>
        /// <param name="InitParams">Params struct for construction routine</param>
        /// <returns></returns>
        public static bool OpenSharpSession(SharpSession SessionType, SessionInit InitParams)
        {
            // Log information about the session being built out here. 
            ConstantsLogger.WriteLog($"BUILDING NEW SHARP SESSION FOR A {SessionType} NOW...", LogType.InfoLog);
            ConstantsLogger.WriteLog($"SESSION PARAMS PROVIDED:\n\tVersion: {InitParams.Version}\n\tDLL:    {InitParams.DLLName}\n\tDevice: {InitParams.DeviceName}");

            // Now build our new session object for our given input values.
        }
    }
}
