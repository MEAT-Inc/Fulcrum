using System.Windows.Controls;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorMiscViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels.InjectorOptionViewModels;
using FulcrumInjector.FulcrumViewContent.FulcrumViews;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorCoreViews;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorMiscViews;
using FulcrumInjector.FulcrumViewContent.FulcrumViews.InjectorOptionViews;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels;
using FulcrumInjector.FulcrumViewSupport.FulcrumModels.SettingsModels;
using SharpWrapper;

namespace FulcrumInjector.FulcrumViewSupport
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
        public static FulcrumSettingsShare FulcrumSettings;                     // Collection of all our settings objects loaded in
        public static readonly FulcrumVersionInfo FulcrumVersions = new();      // Current version information for this application

        // Public static fields for our injector sharp sessions
        public static Sharp2534Session SharpSessionAlpha;                       // Sharp Session used for configuring hardware
        public static Sharp2534Session SharpSessionBravo;                       // Sharp Session for setting up simulations

        // Private static backing field for the injector main window and watchdog
        private static FulcrumMainWindow _fulcrumMainWindow;                    // Main window of the injector application
        
        // Private static Singleton Injector DLL Core Output View Contents. These get set to control view contents on the Main window
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumInstalledHardwareSingleton;
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumDllOutputSingleton;
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumLogReviewSingleton;
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumSimulationSingleton;
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumSettingsPaneSingleton;
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumSessionReportingSingleton;
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumDebugLoggingSingleton;

        #endregion //Fields

        #region Properties

        // Public static property holding the current injector window instance
        public static FulcrumMainWindow FulcrumMainWindow
        {
            get => _fulcrumMainWindow;
            set
            {
                // Store the new main window value on our backing field
                _fulcrumMainWindow = value;

                // Find and store all of our singleton instances now
                _fulcrumInstalledHardwareSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumInstalledHardwareView));
                _fulcrumDllOutputSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumDllOutputLogView));
                _fulcrumLogReviewSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumLogReviewView));
                _fulcrumSimulationSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumSimulationPlaybackView));
                _fulcrumSettingsPaneSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumSettingsPaneView));
                _fulcrumSessionReportingSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumSessionReportingView));
                _fulcrumDebugLoggingSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumDebugLoggingView));
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

        // Public static properties used to configure constant views used in the title view
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
        public static FulcrumGoogleDriveView FulcrumGoogleDriveView
        {
            get => FulcrumMainWindow?.FulcrumGoogleDriveView;
            set => FulcrumMainWindow.FulcrumGoogleDriveView = value;
        }
        public static FulcrumGoogleDriveViewModel FulcrumGoogleDriveViewModel
        {
            get => FulcrumMainWindow?.FulcrumGoogleDriveView.ViewModel;
            set => FulcrumMainWindow.FulcrumGoogleDriveView.ViewModel = value;
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