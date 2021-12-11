using System;
using System.Linq;
using FulcrumInjector.AppLogic;
using FulcrumInjector.AppLogic.InjectorPipes;
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

        // Object Constants for our application
        public static FulcrumPipeReader AlphaPipe;      // Pipe objects for talking to our DLL
        public static FulcrumPipeWriter BravoPipe;      // Pipe objects for talking to our DLL

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
        /// <summary>
        /// Builds our new pipe instances out for this session
        /// </summary>
        public static void ConfigureFulcrumPipes()
        {
            // Configure pipes here.
            ConstantsLogger.WriteLog("SETTING UP FULCRUM PIPES NOW...", LogType.WarnLog);
            InjectorPipeSetup.KillExistingFulcrumInstances();
            if (!InjectorPipeSetup.ValidateFulcrumPipeConfiguration())
                throw new InvalidOperationException("FAILED TO CONFIGURE FULCRUM PIPE INSTANCES!");
            ConstantsLogger.WriteLog("FULCRUM PIPE INSTANCES HAVE BEEN BOOTED CORRECTLY!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
