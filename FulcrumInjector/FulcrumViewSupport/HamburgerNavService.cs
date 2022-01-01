using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using FulcrumInjector.FulcrumViewContent;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

namespace FulcrumInjector.FulcrumViewSupport
{
    /// <summary>
    /// Used for navigating view content from our hamburger view control
    /// </summary>
    public class HamburgerNavService
    {
        // Logger for navigation service instance
        private SubServiceLogger NavLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("HamburgerNavServiceLogger")) ?? new SubServiceLogger("HamburgerNavServiceLogger");

        // Navigation events and event handler
        public event NavigatedEventHandler Navigated;
        public event NavigationFailedEventHandler NavigationFailed;

        // Frame instance used for animating later on
        private Frame _navigationFrame;
        public Frame NavigationFrame
        {
            get
            {
                // If frame is not null, return the current value. 
                if (this._navigationFrame != null) return this._navigationFrame;

                // Build new frame and register it.
                this._navigationFrame = new Frame() { NavigationUIVisibility = NavigationUIVisibility.Hidden };
                this.RegisterFrameEvents();
                return this._navigationFrame;
            }
            set
            {
                // Remove existing frames, store it, and register new ones.
                this.UnregisterFrameEvents();
                this._navigationFrame = value;
                this.RegisterFrameEvents();
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new navigation service instance object
        /// </summary>
        public HamburgerNavService() { this.NavLogger.WriteLog("[NAVIGATION_CTOR] ::: BUILT NEW INSTANCE OF A NAVIGATION SERVICE OK!", LogType.InfoLog); }


        /// <summary>
        /// Navigation method for controlling view based on a URI
        /// </summary>
        /// <param name="UserControlType">Type of view content</param>
        /// <returns>True if view is changed</returns>
        public bool Navigate(Type UserControlType, Type ViewModelType)
        {
            // If not type of view model control base, then dump out.
            if (UserControlType.BaseType != typeof(UserControl)) {
                this.NavLogger.WriteLog($"[NAVIGATE_TYPE] :::CAN NOT USE A NON USERCONTROL BASE TYPE FOR SINGLETON LOOKUPS!", LogType.ErrorLog);
                return false;
            }

            // Find the object we need to use for our content.
            var NavigationOutputContent = SingletonContentControl<UserControl, ViewModelControlBase>.BuiltSingletonInstances
                ?.FirstOrDefault(SingletonObj => SingletonObj?.SingletonUserControl.GetType() == UserControlType)
                ?.SingletonUserControl;

            // Check if our user control object output is defined or not.
            if (NavigationOutputContent == null) {
                this.NavLogger.WriteLog($"[NAVIGATE_TYPE] ::: FAILED TO LOCATE CONTENT FOR TYPE OBJECT: {UserControlType.Name}!", LogType.WarnLog);
                this.NavLogger.WriteLog($"[NAVIGATE_TYPE] ::: BUILDING NEW VIEW CONTENT AND STORING NEW VALUES ON INJECTOR CONSTANTS FOR IT NOW...", LogType.WarnLog);

                // Build instance of the type passed in and have it store into the injector constants
                var BuiltSingleton = SingletonContentControl<UserControl, ViewModelControlBase>.CreateSingletonInstance(UserControlType, ViewModelType);
                this.NavLogger.WriteLog($"[NAVIGATE_TYPE] ::: BUILT NEW OUTPUT CONTENT FOR THE CONTROL TYPE {UserControlType.Name} OK!", LogType.InfoLog);
                this.NavLogger.WriteLog($"[NAVIGATE_TYPE] ::: NAVIGATING TO NEW TYPE: {UserControlType.Name}", LogType.TraceLog);
                return this.NavigationFrame.NavigationService?.Content?.GetType() != UserControlType &&
                       this.NavigationFrame.Navigate(BuiltSingleton.SingletonUserControl);
            }

            // Return true if navigation passed correctly
            this.NavLogger.WriteLog($"[NAVIGATE_TYPE] ::: NAVIGATING TO NEW TYPE: {UserControlType.Name}", LogType.TraceLog);
            return this.NavigationFrame.NavigationService?.Content?.GetType() != UserControlType &&
                   this.NavigationFrame.Navigate(NavigationOutputContent);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Registers a new frame instance onto our navigation helper
        /// </summary>
        private void RegisterFrameEvents()
        {
            // Check for frame state value
            if (this._navigationFrame == null) {
                this.NavLogger.WriteLog($"[NAVIGATE_REGISTER] ::: FAILED TO REGISTER NEW FRAME SINCE IT WAS NULL!", LogType.TraceLog);
                return;
            }

            // Add new event handlers
            this._navigationFrame.Navigated += this.NavigationFrameNavigated;
            this._navigationFrame.NavigationFailed += this.NavigationFrameNavigationFailed;
            this.NavLogger.WriteLog($"[NAVIGATE_REGISTER] ::: REGISTERED NEW FRAME INSTANCE OBJECT", LogType.TraceLog);
        }
        /// <summary>
        /// Removes a frame instance from our frame object store
        /// </summary>
        private void UnregisterFrameEvents()
        {
            // Check for frame state value
            if (this._navigationFrame == null) {
                this.NavLogger.WriteLog($"[NAVIGATE_UNREGISTER] ::: FAILED TO UNREGISTER NEW FRAME SINCE IT WAS NULL!", LogType.TraceLog);
                return;
            }

            // Remove event handlers
            this._navigationFrame.Navigated -= this.NavigationFrameNavigated;
            this._navigationFrame.NavigationFailed -= this.NavigationFrameNavigationFailed;
            this.NavLogger.WriteLog($"[NAVIGATE_UNREGISTER] ::: UNREGISTERED NEW FRAME INSTANCE OBJECT", LogType.TraceLog);
        }


        /// <summary>
        /// Navigation passed event controlling
        /// </summary>
        /// <param name="SendingObject"></param>
        /// <param name="NavEventArgs"></param>
        private void NavigationFrameNavigated(object SendingObject, NavigationEventArgs NavEventArgs) => this.Navigated?.Invoke(SendingObject, NavEventArgs);
        /// <summary>
        /// Event for navigation failed to process
        /// </summary>
        /// <param name="SendingObject"></param>
        /// <param name="NavFailedArgs"></param>
        private void NavigationFrameNavigationFailed(object SendingObject, NavigationFailedEventArgs NavFailedArgs) => this.NavigationFailed?.Invoke(SendingObject, NavFailedArgs);
    }
}
