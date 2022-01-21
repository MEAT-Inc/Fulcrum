using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using FulcrumInjector.FulcrumLogic.JsonHelpers;
using FulcrumInjector.FulcrumViewContent.Models;
using FulcrumInjector.FulcrumViewContent.ViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorCoreViewModels;
using FulcrumInjector.FulcrumViewContent.ViewModels.InjectorOptionViewModels;
using FulcrumInjector.FulcrumViewContent.Views;
using FulcrumInjector.FulcrumViewContent.Views.InjectorCoreViews;
using FulcrumInjector.FulcrumViewContent.Views.InjectorOptionViews;
using Newtonsoft.Json;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewContent
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
        public static InjectorMainWindow InjectorMainWindow { get; private set; }

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

        // Pipe Status View and ViewModel
        public static FulcrumPipeStatusView FulcrumPipeStatusView
        {
            get => InjectorMainWindow.FulcrumPipeStatus;
            set => InjectorMainWindow.FulcrumPipeStatus = value;
        }
        public static FulcrumPipeStatusViewModel FulcrumPipeStatusViewModel
        {
            get => FulcrumPipeStatusView.ViewModel;
            set => FulcrumPipeStatusView.ViewModel = value;
        }

        // OE Applications Installed View and ViewModel
        public static FulcrumInstalledOeAppsView FulcrumInstalledOeAppsView
        {
            get => InjectorMainWindow.FulcrumInstalledOeApps;
            set => InjectorMainWindow.FulcrumInstalledOeApps = value;
        }
        public static FulcrumInstalledOeAppsViewModel FulcrumInstalledOeAppsViewModel
        {
            get => FulcrumInstalledOeAppsView.ViewModel;
            set => FulcrumInstalledOeAppsView.ViewModel = value;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        // Injector Hamburger Output Views
        public static FulcrumHamburgerCoreView FulcrumHamburgerCoreView
        {
            get => InjectorMainWindow.FulcrumHamburgerCore;
            set => InjectorMainWindow.FulcrumHamburgerCore = value;
        }
        public static FulcrumHamburgerCoreViewModel FulcrumHamburgerCoreViewModel
        {
            get => FulcrumHamburgerCoreView.ViewModel;
            set => FulcrumHamburgerCoreView.ViewModel = value;
        }

        // --------------------------------------------------------------------------------------------------------------------------

        // Singleton Injector DLL Core Output View Contents. These get set to control view contents on the Main window
        public static SingletonContentControl<UserControl, ViewModelControlBase> FulcrumInstalledHardwareSingleton =>
            SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumInstalledHardwareView));
        public static SingletonContentControl<UserControl, ViewModelControlBase> FulcrumDllOutputSingleton =>
            SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumDllOutputLogView));
        public static SingletonContentControl<UserControl, ViewModelControlBase> FulcrumLogReviewSingleton =>
            SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumLogReviewView));


        // Singleton Injector DLL Option Output View Contents. These get set to control view contents on the Main window
        public static SingletonContentControl<UserControl, ViewModelControlBase> FulcrumSettingsPaneSingleton =>
            SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSettingsPaneView));
        public static SingletonContentControl<UserControl, ViewModelControlBase> FulcrumSessionReportingSingleton =>
            SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumSessionReportingView));
        public static SingletonContentControl<UserControl, ViewModelControlBase> FulcrumDebugLoggingSingleton =>
            SingletonContentControl<UserControl, ViewModelControlBase>.LocateSingletonViewInstance(typeof(FulcrumDebugLoggingView));

        // --------------------------------------------------------------------------------------------------------------------------

        // Installed Hardware view  user control and view model object
        public static FulcrumInstalledHardwareView FulcrumInstalledHardwareView => (FulcrumInstalledHardwareView)Convert.ChangeType(FulcrumInstalledHardwareSingleton?.SingletonUserControl, typeof(FulcrumInstalledHardwareView));
        public static FulcrumInstalledHardwareViewModel FulcrumInstalledHardwareViewModel => (FulcrumInstalledHardwareViewModel)FulcrumInstalledHardwareView?.ViewModel;

        // DLL Output Logging user control and view model object
        public static FulcrumDllOutputLogView FulcrumDllOutputLogView => (FulcrumDllOutputLogView)Convert.ChangeType(FulcrumDllOutputSingleton?.SingletonUserControl, typeof(FulcrumDllOutputLogView));
        public static FulcrumDllOutputLogViewModel FulcrumDllOutputLogViewModel => (FulcrumDllOutputLogViewModel)FulcrumDllOutputSingleton?.SingletonViewModel;

        // Log Reviewing user control and view model object
        public static FulcrumLogReviewView FulcrumLogReviewView => (FulcrumLogReviewView)Convert.ChangeType(FulcrumLogReviewSingleton?.SingletonUserControl, typeof(FulcrumLogReviewView));
        public static FulcrumLogReviewViewModel FulcrumLogReviewViewModel => (FulcrumLogReviewViewModel)FulcrumLogReviewSingleton?.SingletonViewModel;

        // User settings and configuration user control and view model object
        public static FulcrumSettingsPaneView FulcrumSettingsPaneView => (FulcrumSettingsPaneView)Convert.ChangeType(FulcrumSettingsPaneSingleton?.SingletonUserControl, typeof(FulcrumSettingsPaneView));
        public static FulcrumSettingsPaneViewModel FulcrumSettingsPaneViewModel => (FulcrumSettingsPaneViewModel)FulcrumSettingsPaneSingleton?.SingletonViewModel;

        // Session output reporting user control and view model object
        public static FulcrumSessionReportingView FulcrumSessionReportingView => (FulcrumSessionReportingView)Convert.ChangeType(FulcrumSessionReportingSingleton?.SingletonUserControl, typeof(FulcrumSessionReportingView));
        public static FulcrumSessionReportingViewModel FulcrumSessionReportingViewModel => (FulcrumSessionReportingViewModel)FulcrumSessionReportingSingleton?.SingletonViewModel;

        // Debug logging output user control and view model object
        public static FulcrumDebugLoggingView FulcrumDebugLoggingView => (FulcrumDebugLoggingView)Convert.ChangeType(FulcrumDebugLoggingSingleton?.SingletonUserControl, typeof(FulcrumDebugLoggingView));
        public static FulcrumDebugLoggingViewModel FulcrumDebugLoggingViewModel => (FulcrumDebugLoggingViewModel)FulcrumDebugLoggingSingleton?.SingletonViewModel;


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
            bool SetConstants = FulcrumTitleView.SetFlyoutBindings(
                InjectorMainWindow.InformationFlyout, 
                InjectorMainWindow.CloseInfoFlyoutButton
            ); 
            
            // Check result
            if (SetConstants) ConstantsLogger.WriteLog("STORED VALUES FROM MAIN WINDOW OK!", LogType.InfoLog);
            else throw new InvalidOperationException("FAILED TO CONFIGURE NEW SETTINGS AND DEBUG FLYOUT VIEWS!");

        }
        /// <summary>
        /// Sets a value on one of the global UI Control values here
        /// </summary>
        /// <param name="ViewOrViewModelType"></param>
        /// <param name="PropertyName"></param>
        /// <param name="PropertyValue"></param>
        /// <returns></returns>
        public static bool SetConstantVariable(Type ViewOrViewModelType, string PropertyName, object PropertyValue)
        {
            // Start by finding the control with the type given
            ConstantsLogger.WriteLog($"ATTEMPTING TO SET VAR {PropertyName} ON OBJECT TYPED {ViewOrViewModelType.Name}....");
            var DesiredPropertyObject = ViewOrViewModelType.GetMembers(BindingFlags.Public | BindingFlags.Static)
                .Where(MemberObj => MemberObj.MemberType == MemberTypes.Property)
                .Select(MemberObj =>
                {
                    // Pull value object and cast into property info
                    PropertyInfo CastInfo = MemberObj as PropertyInfo;
                    object ValuePulled = CastInfo.GetValue(null);

                    // Return built tuple
                    return new Tuple<PropertyInfo, string, object>(CastInfo, CastInfo.PropertyType.Name, ValuePulled);
                }).ToList()
                .FirstOrDefault(ValueSet => ValueSet.Item2 == ViewOrViewModelType.Name);

            // Make sure it's not null
            if (DesiredPropertyObject == null) {
                ConstantsLogger.WriteLog("FAILED TO FIND A USABLE PROPERTY OBJECT VALUE ON THE CONSTANTS OBJECT!", LogType.ErrorLog);
                return false;
            }

            // Now apply our new value
            ConstantsLogger.WriteLog("LOCATED NEW PROPERTY OBJECT TO MODIFY OK!", LogType.InfoLog);
            try
            {
                // Pull the member info and store the best one for us
                var DesiredMember = DesiredPropertyObject.Item3.GetType()
                    .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(MemberObj => MemberObj.Name == PropertyName);
                if (DesiredMember == null) throw new InvalidOperationException("FAILED TO LOCATE MEMBER ON REFLECTED INSTANCE");
                ConstantsLogger.WriteLog("PULLED NEW MEMBER INSTANCE OBJECT OK! SETTING IT NOW...", LogType.InfoLog);

                // Now set the value on our new member info
                switch (DesiredMember.MemberType)
                {
                    // Sets the value on the class into the current invoking object
                    case MemberTypes.Field:
                        FieldInfo InvokerField = (FieldInfo)DesiredMember;
                        InvokerField.SetValue(DesiredPropertyObject.Item3, PropertyValue);
                        break;

                    // PropertyInfo
                    case MemberTypes.Property:
                        PropertyInfo InvokerProperty = (PropertyInfo)DesiredMember;
                        InvokerProperty.SetValue(DesiredPropertyObject.Item3, PropertyValue);
                        break;

                    // Not found
                    default: throw new NotImplementedException($"THE INVOKED MEMBER {PropertyName} COULD NOT BE FOUND!");
                }

                // Set new value correctly! Log and return passed
                ConstantsLogger.WriteLog("SET NEW VALUE OBJECT TO OUR DESIRED PROPERTY OK!", LogType.InfoLog);
                return true;
            }
            catch (Exception Ex)
            {
                // Catch failure, log it, and return failed
                ConstantsLogger.WriteLog($"FAILED TO SET NEW PROPERTY VALUE NAMED {PropertyName}!", LogType.TraceLog);
                ConstantsLogger.WriteLog("EXCEPTION THROWN DURING PULL!", Ex);
                return false;
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------
    }
}