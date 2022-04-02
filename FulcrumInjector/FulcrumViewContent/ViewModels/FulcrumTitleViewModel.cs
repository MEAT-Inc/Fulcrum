using System.Linq;
using System.Reflection;
using FulcrumInjector.FulcrumLogic.JsonLogic.JsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent.ViewModels
{
    /// <summary>
    /// ViewModel logic for our title view component
    /// </summary>
    public class FulcrumTitleViewModel : ViewModelControlBase
    {
        // Logger object.
        private static SubServiceLogger ViewModelLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("TitleViewModelLogger")) ?? new SubServiceLogger("TitleViewModelLogger");

        // Private control values
        private string _titleTextString;        // Private value for title view title text
        private string _titleVersionString;     // Private value for title view version text

        // Title string and the title view version bound values
        public string TitleTextString { get => _titleTextString; set => PropertyUpdated(value); }
        public string TitleVersionString { get => _titleVersionString; set => PropertyUpdated(value); }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new VM and generates a new logger object for it.
        /// </summary>
        public FulcrumTitleViewModel()
        {
            // Log information and store values 
            ViewModelLogger.WriteLog($"VIEWMODEL LOGGER FOR VM {this.GetType().Name} HAS BEEN STARTED OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog("SETTING UP TITLE VIEW BOUND VALUES NOW...", LogType.WarnLog);

            // Store title and version string values now.
            this.TitleVersionString = $"Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            this.TitleTextString = ValueLoaders.GetConfigValue<string>("FulcrumInjectorConstants.AppInstanceName");
            ViewModelLogger.WriteLog("PULLED NEW TITLE AND VERSION VALUES OK!", LogType.InfoLog);
            ViewModelLogger.WriteLog($"TITLE:    {TitleTextString}");
            ViewModelLogger.WriteLog($"VERSION:  {TitleVersionString}");

            // Log completed setup.
            ViewModelLogger.WriteLog("SETUP NEW TITLE AND VERSION STRING VALUES OK!", LogType.InfoLog);
        }
    }
}
