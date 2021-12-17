using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.AppLogic.InjectorPipes;
using FulcrumInjector.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl.Models
{
    /// <summary>
    /// Model object for our pipe controls and status values
    /// </summary>
    public class FulcrumPipeSystemModel
    {
        // Logger object for the pipe injection application
        private static SubServiceLogger PipeStatusModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("PipeStatusModelLogger")) ?? new SubServiceLogger("PipeStatusModelLogger");

        // Object Constants for our application
        public FulcrumPipeReader AlphaPipe;      // Pipe objects for talking to our DLL
        public FulcrumPipeWriter BravoPipe;      // Pipe objects for talking to our DLL

        // -------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds and sets up a new set of fulcrum pipe objects
        /// </summary>
        /// <returns>True if pipes are built. False if not.</returns>
        public bool ValidateFulcrumPipeConfiguration()
        {
            // Reset pipe configuration if needed
            PipeStatusModelLogger.WriteLog("RESETTING PIPE CONFIGURATION VALUES NOW...", LogType.InfoLog);
            FulcrumPipeReader.ResetPipeInstance(); 
            FulcrumPipeWriter.ResetPipeInstance();

            // Main pipes for the fulcrum application
            PipeStatusModelLogger.WriteLog("BUILDING NEW PIPE OBJECTS NOW...", LogType.InfoLog);
            this.AlphaPipe = FulcrumPipeReader.PipeInstance;
            this.BravoPipe = FulcrumPipeWriter.PipeInstance;

            // Output Pipe objects built.
            bool OutputResult = new FulcrumPipe[] { this.AlphaPipe, this.BravoPipe }.All(PipeObj => PipeObj.PipeState == FulcrumPipeState.Connected);
            if (OutputResult)
            {
                // Store pipes from our connection routine
                PipeStatusModelLogger.WriteLog("BUILT NEW PIPE SERVERS FOR BOTH ALPHA AND BRAVO WITHOUT ISSUE!", LogType.InfoLog);
                PipeStatusModelLogger.WriteLog("PIPES ARE OPEN AND STORED CORRECTLY! READY TO PROCESS OR SEND DATA THROUGH THEM!", LogType.InfoLog);
                return true;
            }

            // Log this failure then exit the application
            PipeStatusModelLogger.WriteLog("FAILED TO BUILD ONE OR BOTH PIPE SERVER READING CLIENTS!", LogType.FatalLog);
            PipeStatusModelLogger.WriteLog("FAILED TO CONFIGURE ONE OR MORE OF THE PIPE OBJECTS FOR THIS SESSION!", LogType.FatalLog);
            return false;
        }
    }
}
