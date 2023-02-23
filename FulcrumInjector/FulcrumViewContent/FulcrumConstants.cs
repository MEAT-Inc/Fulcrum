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

        // Static fields for our injector application information
        private static FulcrumSettingsShare _fulcrumSettings;                   // Collection of all setting objects for this instance
        public static readonly FulcrumVersionInfo FulcrumVersions = new();      // Current version information for this application

        // Public static fields for our injector sharp sessions
        public static Sharp2534Session SharpSessionAlpha;                       // Sharp Session used for configuring hardware
        public static Sharp2534Session SharpSessionBravo;                       // Sharp Session for setting up simulations

        // Private static backing field for the injector main window
        private static FulcrumMainWindow _fulcrumMainWindow;                    // Main window of the injector application

        // Private static Singleton Injector DLL Core Output View Contents. These get set to control view contents on the Main window
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumInstalledHardwareFulcrumSingleton;
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumDllOutputFulcrumSingleton;
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumLogReviewFulcrumSingleton;
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumSimulationFulcrumSingleton;
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumSettingsPaneFulcrumSingleton;
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumSessionReportingFulcrumSingleton;
        private static FulcrumSingletonContent<UserControl, ViewModelControlBase> _fulcrumDebugLoggingFulcrumSingleton;

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
                _fulcrumInstalledHardwareFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumInstalledHardwareView));
                _fulcrumDllOutputFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumDllOutputLogView));
                _fulcrumLogReviewFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumLogReviewView));
                _fulcrumSimulationFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSimulationPlaybackView));
                _fulcrumSettingsPaneFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSettingsPaneView));
                _fulcrumSessionReportingFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSessionReportingView));
                _fulcrumDebugLoggingFulcrumSingleton = FulcrumSingletonContent<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumDebugLoggingView));
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
        public static FulcrumInstalledHardwareView FulcrumInstalledHardwareView => (FulcrumInstalledHardwareView)_fulcrumInstalledHardwareFulcrumSingleton?.SingletonUserControl;
        public static FulcrumInstalledHardwareViewModel FulcrumInstalledHardwareViewModel => (FulcrumInstalledHardwareViewModel)_fulcrumInstalledHardwareFulcrumSingleton?.SingletonViewModel;
        public static FulcrumDllOutputLogView FulcrumDllOutputLogView => (FulcrumDllOutputLogView)_fulcrumDllOutputFulcrumSingleton?.SingletonUserControl;
        public static FulcrumDllOutputLogViewModel FulcrumDllOutputLogViewModel => (FulcrumDllOutputLogViewModel)_fulcrumDllOutputFulcrumSingleton?.SingletonViewModel;
        public static FulcrumLogReviewView FulcrumLogReviewView => (FulcrumLogReviewView)_fulcrumLogReviewFulcrumSingleton?.SingletonUserControl;
        public static FulcrumLogReviewViewModel FulcrumLogReviewViewModel => (FulcrumLogReviewViewModel)_fulcrumLogReviewFulcrumSingleton?.SingletonViewModel;
        public static FulcrumSimulationPlaybackView FulcrumSimulationPlaybackView => (FulcrumSimulationPlaybackView)_fulcrumSimulationFulcrumSingleton?.SingletonUserControl;
        public static FulcrumSimulationPlaybackViewModel FulcrumSimulationPlaybackViewModel => (FulcrumSimulationPlaybackViewModel)_fulcrumSimulationFulcrumSingleton?.SingletonViewModel;
        public static FulcrumSettingsPaneView FulcrumSettingsPaneView => (FulcrumSettingsPaneView)_fulcrumSettingsPaneFulcrumSingleton?.SingletonUserControl;
        public static FulcrumSettingsPaneViewModel FulcrumSettingsPaneViewModel => (FulcrumSettingsPaneViewModel)_fulcrumSettingsPaneFulcrumSingleton?.SingletonViewModel;
        public static FulcrumSessionReportingView FulcrumSessionReportingView => (FulcrumSessionReportingView)_fulcrumSessionReportingFulcrumSingleton?.SingletonUserControl;
        public static FulcrumSessionReportingViewModel FulcrumSessionReportingViewModel => (FulcrumSessionReportingViewModel)_fulcrumSessionReportingFulcrumSingleton?.SingletonViewModel;
        public static FulcrumDebugLoggingView FulcrumDebugLoggingView => (FulcrumDebugLoggingView)_fulcrumDebugLoggingFulcrumSingleton?.SingletonUserControl;
        public static FulcrumDebugLoggingViewModel FulcrumDebugLoggingViewModel => (FulcrumDebugLoggingViewModel)_fulcrumDebugLoggingFulcrumSingleton?.SingletonViewModel;

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
    }
}