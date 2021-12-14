using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static FulcrumTitleView FulcrumTitleView
        {
            get => InjectorMainWindow.FulcrumTitle;
            set => InjectorMainWindow.FulcrumTitle = value;
        }
        public static FulcrumTitleViewModel FulcrumTitleViewModel
        {
            get => FulcrumTitleView.ViewModel;
            set => FulcrumTitleView.ViewModel = value;
        }

        // Test DLL Injector View and ViewModel
        public static FulcrumDllInjectionTestView FulcrumDllInjectionTestView
        {
            get => InjectorMainWindow.FulcrumDllInjectionTest;
            set => InjectorMainWindow.FulcrumDllInjectionTest = value;
        }
        public static FulcrumDllInjectionTestViewModel FulcrumDllInjectionTestViewModel
        {
            get => FulcrumDllInjectionTestView.ViewModel;
            set => FulcrumDllInjectionTestView.ViewModel = value;
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

            // Set the flyouts for our debugging configuration and settings pane
            ConstantsLogger.WriteLog("STORING VIEWS FOR SETTINGS AND DEBUG FLYOUTS NOW...");
            if (FulcrumTitleView.SetFlyoutBindings(InjectorMainWindow.SettingsViewFlyout, InjectorMainWindow.DebugViewFlyout))
                ConstantsLogger.WriteLog("STORED VALUES FROM MAIN WINDOW OK!", LogType.InfoLog);
            else throw new InvalidOperationException("FAILED TO CONFIGURE NEW SETTINGS AND DEBUG FLYOUT VIEWS!");
        }

        /// <summary>
        /// Builds our new pipe instances out for this session
        /// </summary>
        public static bool ConfigureFulcrumPipes()
        {
            // Configure pipes here.
            ConstantsLogger.WriteLog("SETTING UP FULCRUM PIPES NOW...", LogType.WarnLog);

            // Kill old, boot new pipe instances.
            InjectorPipeSetup.KillExistingFulcrumInstances();
            bool PipeConfig = InjectorPipeSetup.ValidateFulcrumPipeConfiguration();
            if (!PipeConfig) ConstantsLogger.WriteLog("FAILED TO CONFIGURE FULCRUM PIPE INSTANCES!", LogType.FatalLog);
            else ConstantsLogger.WriteLog("FULCRUM PIPE INSTANCES HAVE BEEN BOOTED CORRECTLY!", LogType.InfoLog);

            // Log done. Results are variable here.
            ConstantsLogger.WriteLog("FULCRUM PIPE CONFIGURATION HAS BEEN COMPLETED. CHECK THE UI AND LOG FILES FOR RESULTS", LogType.WarnLog);
            return PipeConfig;
        }

        // --------------------------------------------------------------------------------------------------------------------------
    }
}