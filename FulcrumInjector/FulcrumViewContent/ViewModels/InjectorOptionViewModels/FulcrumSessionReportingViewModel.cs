using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels
{
    /// <summary>
    /// Session reporting ViewModel used for sending cleaned up session information out to my email
    /// Email is defined in the app settings.
    /// </summary>
    public class FulcrumSessionReportingViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("SessionReportViewModelLogger")) ?? new SubServiceLogger("SessionReportViewModelLogger");

        // Private Control Values
        private readonly string _reportingEmailSender;
        private readonly string _reportingEmailRecipients;

        // Public values for our view to bind onto 


        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumSessionReportingViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP REPORTING VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store email configuration values
            this._reportingEmailSender = ValueLoaders.GetConfigValue<string>("")


            ViewModelLogger.WriteLog("BUILT NEW MODEL OBJECT AND WATCHDOG OBJECTS FOR PIPE INSTANCES OK!", LogType.InfoLog);

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW PIPE STATUS MONITOR VALUES OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
