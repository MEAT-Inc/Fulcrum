using System.Linq;
using FulcrumInjector.ViewControl.ViewModels;
using FulcrumInjector.ViewControl.Views;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.ViewControl
{
    /// <summary>
    /// Static class which holds all the View constants for our application.
    /// </summary>
    public static class InjectorConstants
    {
        // Logger object.
        private static SubServiceLogger ConstantsLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("WatchdogConstantsLogger")) ?? new SubServiceLogger("WatchdogConstantsLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        // View Constants for help with property bindings
        public static InjectorMainWindow InjectorMainWindow { get; private set; }     // Main window component    

        // Title View and ViewModel
        public static FulcrumTitleView TitleView 
        {
            get => InjectorMainWindow.FulcrumTitleViewContent;
            set => InjectorMainWindow.FulcrumTitleViewContent = value;
        }
        public static FulcrumTitleViewModel TitleViewModel 
        {
            get => TitleView.ViewModel;
            set => TitleView.ViewModel = value;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a static set of control objects for view use
        /// </summary>
        /// <param name="WindowBase">Main window being controlled</param>
        public static void ConfigureViewControls(InjectorMainWindow WindowBase)
        {
            // Store value and log results
            InjectorMainWindow = WindowBase;
            ConstantsLogger.WriteLog("STORED NEW MAIN WINDOW VIEW FOR CONSTANTS OBJECT OK!", LogType.InfoLog);
            ConstantsLogger.WriteLog($"MAIN WINDOW WAS PASSED AS TYPE {WindowBase.GetType().Name}");
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
