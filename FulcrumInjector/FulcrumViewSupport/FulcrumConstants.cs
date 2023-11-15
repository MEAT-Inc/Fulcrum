using System;
using System.Linq;
using System.Reflection;
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
using SharpLogging;
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
        
        // Public static fields for our injector sharp sessions
        public static Sharp2534Session SharpSessionAlpha;                       // Sharp Session used for configuring hardware
        public static Sharp2534Session SharpSessionBravo;                       // Sharp Session for setting up simulations

        #endregion //Fields

        #region Properties

        // Public static property holding the current injector window instance
        public static FulcrumMainWindow FulcrumMainWindow { get; set; }

        // Public static properties used to help configure our new instances of windows and views for the injector app
        public static FulcrumDllInjectionTestView FulcrumDllInjectionTestView
        {
            get => FulcrumMainWindow?.FulcrumDllInjectionTest;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumMainWindow == null) return;
                FulcrumMainWindow.FulcrumDllInjectionTest = value;
            }
        }
        public static FulcrumDllInjectionTestViewModel FulcrumDllInjectionTestViewModel
        {
            get => FulcrumDllInjectionTestView?.ViewModel;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumDllInjectionTestView == null) return;
                FulcrumDllInjectionTestView.ViewModel = value;
            }
        }
        public static FulcrumInstalledOeAppsView FulcrumInstalledOeAppsView
        {
            get => FulcrumMainWindow?.FulcrumInstalledOeApps;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumMainWindow == null) return;
                FulcrumMainWindow.FulcrumInstalledOeApps = value;
            }
        }
        public static FulcrumInstalledOeAppsViewModel FulcrumInstalledOeAppsViewModel
        {
            get => FulcrumInstalledOeAppsView?.ViewModel;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumInstalledOeAppsView == null) return;
                FulcrumInstalledOeAppsView.ViewModel = value;
            }
        }
        public static FulcrumPipeStatusView FulcrumPipeStatusView
        {
            get => FulcrumMainWindow?.FulcrumPipeStatus;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumMainWindow == null) return;
                FulcrumMainWindow.FulcrumPipeStatus = value;
            }
        }
        public static FulcrumPipeStatusViewModel FulcrumPipeStatusViewModel
        {
            get => FulcrumPipeStatusView?.ViewModel;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumPipeStatusView == null) return;
                FulcrumPipeStatusView.ViewModel = value;
            }
        }
        public static FulcrumTitleView FulcrumTitleView
        {
            get => FulcrumMainWindow?.FulcrumTitle;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumMainWindow == null) return;
                FulcrumMainWindow.FulcrumTitle = value;
            }
        }
        public static FulcrumTitleViewModel FulcrumTitleViewModel
        {
            get => FulcrumTitleView?.ViewModel;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumTitleView == null) return;
                FulcrumTitleView.ViewModel = value;
            }
        }                                      
        public static FulcrumVehicleConnectionInfoView FulcrumVehicleConnectionInfoView
        {
            get => FulcrumMainWindow?.FulcrumVehicleConnectionInfo;
            set
            {
                // Only update this value if our object is not null
                if (FulcrumMainWindow == null) return;
                FulcrumMainWindow.FulcrumVehicleConnectionInfo = value;
            }
        }                
        public static FulcrumVehicleConnectionInfoViewModel FulcrumVehicleConnectionInfoViewModel
        {
            get => FulcrumVehicleConnectionInfoView?.ViewModel;
            set
            {                
                // Only update this value if our object is not null
                if (FulcrumVehicleConnectionInfoView == null) return;
                FulcrumVehicleConnectionInfoView.ViewModel = value;
            }
        }

        // Private static Singleton instances for Injector Core Content
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumDllOutputLogSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumDllOutputLogView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumHamburgerCoreSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumHamburgerCoreView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumInstalledHardwareSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumInstalledHardwareView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumLogReviewSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumLogReviewView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumSimulationPlaybackSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumSimulationPlaybackView));

        // Private static Singleton instances for Injector Miscellaneous Content
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumAboutThisAppSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumAboutThisAppView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumGoogleDriveSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumGoogleDriveView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumUpdaterSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumUpdaterView));

        // Private static Singleton instances for Injector Options Content
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumSettingsSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumSettingsView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumSessionReportingSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumSessionReportingView));
        private static FulcrumSingletonContent<UserControl, FulcrumViewModelBase> _fulcrumDebugLoggingSingleton =>
            FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.LocateSingletonViewInstance(typeof(FulcrumDebugLoggingView));

        // Public facing singleton views and view models for our Injector Core Content
        public static FulcrumDllOutputLogView FulcrumDllOutputLogView => (FulcrumDllOutputLogView)_fulcrumDllOutputLogSingleton?.SingletonUserControl;
        public static FulcrumDllOutputLogViewModel FulcrumDllOutputLogViewModel => (FulcrumDllOutputLogViewModel)_fulcrumDllOutputLogSingleton?.SingletonViewModel;
        public static FulcrumHamburgerCoreView FulcrumHamburgerCoreView => (FulcrumHamburgerCoreView)_fulcrumHamburgerCoreSingleton?.SingletonUserControl;
        public static FulcrumHamburgerCoreViewModel FulcrumHamburgerCoreViewModel => (FulcrumHamburgerCoreViewModel)_fulcrumHamburgerCoreSingleton.SingletonViewModel;
        public static FulcrumInstalledHardwareView FulcrumInstalledHardwareView => (FulcrumInstalledHardwareView)_fulcrumInstalledHardwareSingleton?.SingletonUserControl;
        public static FulcrumInstalledHardwareViewModel FulcrumInstalledHardwareViewModel => (FulcrumInstalledHardwareViewModel)_fulcrumInstalledHardwareSingleton?.SingletonViewModel;
        public static FulcrumLogReviewView FulcrumLogReviewView => (FulcrumLogReviewView)_fulcrumLogReviewSingleton?.SingletonUserControl;
        public static FulcrumLogReviewViewModel FulcrumLogReviewViewModel => (FulcrumLogReviewViewModel)_fulcrumLogReviewSingleton?.SingletonViewModel;
        public static FulcrumSimulationPlaybackView FulcrumSimulationPlaybackView => (FulcrumSimulationPlaybackView)_fulcrumSimulationPlaybackSingleton?.SingletonUserControl;
        public static FulcrumSimulationPlaybackViewModel FulcrumSimulationPlaybackViewModel => (FulcrumSimulationPlaybackViewModel)_fulcrumSimulationPlaybackSingleton?.SingletonViewModel;

        // Public facing singleton views and view models for our Injector Miscellaneous Content
        public static FulcrumAboutThisAppView FulcrumAboutThisAppView => (FulcrumAboutThisAppView)_fulcrumAboutThisAppSingleton.SingletonUserControl;
        public static FulcrumAboutThisAppViewModel FulcrumAboutThisAppViewModel => (FulcrumAboutThisAppViewModel)_fulcrumAboutThisAppSingleton.SingletonViewModel;
        public static FulcrumUpdaterView FulcrumUpdaterView => (FulcrumUpdaterView)_fulcrumUpdaterSingleton.SingletonUserControl;
        public static FulcrumUpdaterViewModel FulcrumUpdaterViewModel => (FulcrumUpdaterViewModel)_fulcrumUpdaterSingleton.SingletonViewModel;
        public static FulcrumGoogleDriveView FulcrumGoogleDriveView => (FulcrumGoogleDriveView)_fulcrumGoogleDriveSingleton.SingletonUserControl;
        public static FulcrumGoogleDriveViewModel FulcrumGoogleDriveViewModel => (FulcrumGoogleDriveViewModel)_fulcrumGoogleDriveSingleton.SingletonViewModel;

        // Public facing singleton views and view models for our Injector Options Content
        public static FulcrumSettingsView FulcrumSettingsView => (FulcrumSettingsView)_fulcrumSettingsSingleton.SingletonUserControl;
        public static FulcrumSettingsViewModel FulcrumSettingsViewModel => (FulcrumSettingsViewModel)_fulcrumSettingsSingleton.SingletonViewModel;
        public static FulcrumSessionReportingView FulcrumSessionReportingView => (FulcrumSessionReportingView)_fulcrumSessionReportingSingleton.SingletonUserControl;
        public static FulcrumSessionReportingViewModel FulcrumSessionReportingViewModel => (FulcrumSessionReportingViewModel)_fulcrumSessionReportingSingleton.SingletonViewModel;
        public static FulcrumDebugLoggingView FulcrumDebugLoggingView => (FulcrumDebugLoggingView)_fulcrumDebugLoggingSingleton.SingletonUserControl;
        public static FulcrumDebugLoggingViewModel FulcrumDebugLoggingViewModel => (FulcrumDebugLoggingViewModel)_fulcrumDebugLoggingSingleton.SingletonViewModel;

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes
    }
}