using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.Models.SettingsModels;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorMiscViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using FulcrumInjector.FulcrumViewContent.Views;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewContent.Views.InjectorMiscViews;
using FulcrumInjector.FulcrumViewContent.Views.InjectorOptionViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumJson.JsonHelpers;
using FulcrumInjector.FulcrumViewSupport.FulcrumUpdater;
using SharpLogging;
using SharpWrapper;

namespace FulcrumInjector.FulcrumViewContent
{
    /// <summary>
    /// Static class which holds all the View constants for our application.
    /// </summary>
    internal static class FulcrumConstants
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private logging instance used to log state changes in the constants class
        private static readonly SharpLogger ConstantsLogger = new(LoggerActions.UniversalLogger);

        // Static fields for our injector application information
        private static FulcrumSettingsShare _fulcrumSettings;                   // Collection of all setting objects for this instance
        public static readonly FulcrumVersionInfo FulcrumVersions = new();      // Current version information for this application

        // Public static fields for our injector sharp sessions
        public static Sharp2534Session SharpSessionAlpha;                       // Sharp Session used for configuring hardware
        public static Sharp2534Session SharpSessionBravo;                       // Sharp Session for setting up simulations

        // Private static backing field for the injector main window
        private static FulcrumMainWindow _fulcrumMainWindow;                    // Main window of the injector application

        // Private static Singleton Injector DLL Core Output View Contents. These get set to control view contents on the Main window
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumInstalledHardwareSingleton;
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumDllOutputSingleton;
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumLogReviewSingleton;
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumSimulationSingleton;
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumSettingsPaneSingleton;
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumSessionReportingSingleton;
        private static SingletonContentControl<UserControl, ViewModelControlBase> _fulcrumDebugLoggingSingleton;

        #endregion //Fields

        #region Properties

        // Public static setting share object for this instance of our injector app
        public static FulcrumSettingsShare FulcrumSettings
        {
            get
            {
                // If the settings share exists, then just return it. Otherwise only build one if possible
                if (_fulcrumSettings != null) return _fulcrumSettings;
                if (string.IsNullOrWhiteSpace(JsonConfigFiles.AppConfigFile)) return null;

                // Return a new instance of the setting share if needed
                return _fulcrumSettings ??= new FulcrumSettingsShare();
            }
        }

        // Public static property holding the current injector window instance
        public static FulcrumMainWindow FulcrumMainWindow
        {
            get => _fulcrumMainWindow;
            set
            {
                // Store the new main window value on our backing field
                _fulcrumMainWindow = value;

                // Find and store all of our singleton instances now
                _fulcrumInstalledHardwareSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumInstalledHardwareView));
                _fulcrumDllOutputSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumDllOutputLogView));
                _fulcrumLogReviewSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumLogReviewView));
                _fulcrumSimulationSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSimulationPlaybackView));
                _fulcrumSettingsPaneSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSettingsPaneView));
                _fulcrumSessionReportingSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSessionReportingView));
                _fulcrumDebugLoggingSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumDebugLoggingView));

                // Log done with this configuration once complete
                ConstantsLogger.WriteLog("STORED VALUES FROM MAIN WINDOW AND CONFIGURED NEW CONSTANTS VALUES FOR SINGLETONS CORRECTLY!", LogType.InfoLog);
            }
        }

        // Public static properties for our injector hamburger view content
        public static FulcrumHamburgerCoreView FulcrumHamburgerCoreView
        {
            get => FulcrumMainWindow?.FulcrumHamburgerCore;
            set => FulcrumMainWindow.FulcrumHamburgerCore = value;
        }
        public static FulcrumHamburgerCoreViewModel FulcrumHamburgerCoreViewModel
        {
            get => FulcrumHamburgerCoreView.ViewModel;
            set => FulcrumHamburgerCoreView.ViewModel = value;
        }

        // Public static properties used to help configure our new instances of windows and views for the injector app
        public static FulcrumTitleView FulcrumTitleView
        {
            get => FulcrumMainWindow?.FulcrumTitle;
            set => FulcrumMainWindow.FulcrumTitle = value;
        }                                                
        public static FulcrumTitleViewModel FulcrumTitleViewModel
        {
            get => FulcrumTitleView?.ViewModel;
            set => FulcrumTitleView.ViewModel = value;
        }                                      
        public static FulcrumVehicleConnectionInfoView FulcrumVehicleConnectionInfoView
        {
            get => FulcrumMainWindow?.FulcrumVehicleConnectionInfo;
            set => FulcrumMainWindow.FulcrumVehicleConnectionInfo = value;
        }                
        public static FulcrumVehicleConnectionInfoViewModel FulcrumVehicleConnectionInfoViewModel
        {
            get => FulcrumVehicleConnectionInfoView?.ViewModel;
            set => FulcrumVehicleConnectionInfoView.ViewModel = value;
        }      
        public static FulcrumDllInjectionTestView FulcrumDllInjectionTestView
        {
            get => FulcrumMainWindow?.FulcrumDllInjectionTest;
            set => FulcrumMainWindow.FulcrumDllInjectionTest = value;
        }                          
        public static FulcrumDllInjectionTestViewModel FulcrumDllInjectionTestViewModel
        {
            get => FulcrumDllInjectionTestView.ViewModel;
            set => FulcrumDllInjectionTestView.ViewModel = value;
        }                
        public static FulcrumPipeStatusView FulcrumPipeStatusView
        {
            get => FulcrumMainWindow?.FulcrumPipeStatus;
            set => FulcrumMainWindow.FulcrumPipeStatus = value;
        }
        public static FulcrumPipeStatusViewModel FulcrumPipeStatusViewModel
        {
            get => FulcrumPipeStatusView.ViewModel;
            set => FulcrumPipeStatusView.ViewModel = value;
        }
        public static FulcrumInstalledOeAppsView FulcrumInstalledOeAppsView
        {
            get => FulcrumMainWindow?.FulcrumInstalledOeApps;
            set => FulcrumMainWindow.FulcrumInstalledOeApps = value;
        }
        public static FulcrumInstalledOeAppsViewModel FulcrumInstalledOeAppsViewModel
        {
            get => FulcrumInstalledOeAppsView.ViewModel;
            set => FulcrumInstalledOeAppsView.ViewModel = value;
        }
        public static FulcrumAboutThisAppView FulcrumAboutThisAppView
        {
            get => FulcrumMainWindow?.FulcrumAboutThisAppView;
            set => FulcrumMainWindow.FulcrumAboutThisAppView = value;
        }
        public static FulcrumAboutThisAppViewModel FulcrumAboutThisAppViewModel
        {
            get => FulcrumMainWindow?.FulcrumAboutThisAppView.ViewModel;
            set => FulcrumMainWindow.FulcrumAboutThisAppView.ViewModel = value;
        }
        public static FulcrumUpdaterView FulcrumUpdaterView
        {
            get => FulcrumMainWindow?.FulcrumUpdaterView;
            set => FulcrumMainWindow.FulcrumUpdaterView = value;
        }
        public static FulcrumUpdaterViewModel FulcrumUpdaterViewModel
        {
            get => FulcrumMainWindow?.FulcrumUpdaterView.ViewModel;
            set => FulcrumMainWindow.FulcrumUpdaterView.ViewModel = value;
        }

        // Public facing singletons used to pull information about our views and view models in the hamburger content
        public static FulcrumInstalledHardwareView FulcrumInstalledHardwareView => (FulcrumInstalledHardwareView)_fulcrumInstalledHardwareSingleton?.SingletonUserControl;
        public static FulcrumInstalledHardwareViewModel FulcrumInstalledHardwareViewModel => (FulcrumInstalledHardwareViewModel)_fulcrumInstalledHardwareSingleton?.SingletonViewModel;
        public static FulcrumDllOutputLogView FulcrumDllOutputLogView => (FulcrumDllOutputLogView)_fulcrumDllOutputSingleton?.SingletonUserControl;
        public static FulcrumDllOutputLogViewModel FulcrumDllOutputLogViewModel => (FulcrumDllOutputLogViewModel)_fulcrumDllOutputSingleton?.SingletonViewModel;
        public static FulcrumLogReviewView FulcrumLogReviewView => (FulcrumLogReviewView)_fulcrumLogReviewSingleton?.SingletonUserControl;
        public static FulcrumLogReviewViewModel FulcrumLogReviewViewModel => (FulcrumLogReviewViewModel)_fulcrumLogReviewSingleton?.SingletonViewModel;
        public static FulcrumSimulationPlaybackView FulcrumSimulationPlaybackView => (FulcrumSimulationPlaybackView)_fulcrumSimulationSingleton?.SingletonUserControl;
        public static FulcrumSimulationPlaybackViewModel FulcrumSimulationPlaybackViewModel => (FulcrumSimulationPlaybackViewModel)_fulcrumSimulationSingleton?.SingletonViewModel;
        public static FulcrumSettingsPaneView FulcrumSettingsPaneView => (FulcrumSettingsPaneView)_fulcrumSettingsPaneSingleton?.SingletonUserControl;
        public static FulcrumSettingsPaneViewModel FulcrumSettingsPaneViewModel => (FulcrumSettingsPaneViewModel)_fulcrumSettingsPaneSingleton?.SingletonViewModel;
        public static FulcrumSessionReportingView FulcrumSessionReportingView => (FulcrumSessionReportingView)_fulcrumSessionReportingSingleton?.SingletonUserControl;
        public static FulcrumSessionReportingViewModel FulcrumSessionReportingViewModel => (FulcrumSessionReportingViewModel)_fulcrumSessionReportingSingleton?.SingletonViewModel;
        public static FulcrumDebugLoggingView FulcrumDebugLoggingView => (FulcrumDebugLoggingView)_fulcrumDebugLoggingSingleton?.SingletonUserControl;
        public static FulcrumDebugLoggingViewModel FulcrumDebugLoggingViewModel => (FulcrumDebugLoggingViewModel)_fulcrumDebugLoggingSingleton?.SingletonViewModel;

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
    }
}