using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Navigation;
using FulcrumInjector.FulcrumViewContent.FulcrumViewModels;
using SharpLogging;

namespace FulcrumInjector.FulcrumViewSupport.FulcrumDataConverters
{
    /// <summary>
    /// Used for navigating view content from our hamburger view control
    /// </summary>
    internal class HamburgerNavService
    {
        #region Custom Events

        // Navigation events for navigation and navigation failed
        public event NavigatedEventHandler Navigated;
        public event NavigationFailedEventHandler NavigationFailed;

        /// <summary>
        /// Navigation passed event controlling
        /// </summary>
        /// <param name="SendingObject"></param>
        /// <param name="NavEventArgs"></param>
        private void OnNavigationFrameNavigated(object SendingObject, NavigationEventArgs NavEventArgs) => this.Navigated?.Invoke(SendingObject, NavEventArgs);
        /// <summary>
        /// Event for navigation failed to process
        /// </summary>
        /// <param name="SendingObject"></param>
        /// <param name="NavFailedArgs"></param>
        private void OnNavigationFrameNavigationFailed(object SendingObject, NavigationFailedEventArgs NavFailedArgs) => this.NavigationFailed?.Invoke(SendingObject, NavFailedArgs);

        #endregion //Custom Events

        #region Fields

        // Logger object and backing fields for our frame setup
        private Frame _navigationFrame;
        private readonly SharpLogger _navServiceLogger = new(LoggerActions.UniversalLogger);

        #endregion //Fields

        #region Properties

        // Frame instance used for animating later on
        public Frame NavigationFrame
        {
            get
            {
                // If frame is not null, return the current value. 
                if (this._navigationFrame != null) return this._navigationFrame;

                // Build new frame and register it.
                this._navigationFrame = new Frame() { NavigationUIVisibility = NavigationUIVisibility.Hidden };
                this._registerFrameEvents();
                return this._navigationFrame;
            }
            set
            {
                // Remove existing frames, store it, and register new ones.
                this._unregisterFrameEvents();
                this._navigationFrame = value;
                this._registerFrameEvents();
            }
        }

        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // --------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Build new navigation service instance object
        /// </summary>
        public HamburgerNavService()
        {
            // Log out this service has been constructed and exit out
            this._navServiceLogger.WriteLog("[NAVIGATION_CTOR] ::: BUILT NEW INSTANCE OF A NAVIGATION SERVICE OK!", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Navigation method for controlling view based on a URI
        /// </summary>
        /// <param name="UserControlType">Type of view content</param>
        /// <returns>True if view is changed</returns>
        public bool Navigate(Type UserControlType, Type ViewModelType)
        {
            // If not type of view model control base, then dump out.
            if (UserControlType.BaseType != typeof(UserControl)) {
                this._navServiceLogger.WriteLog($"[NAVIGATE_TYPE] ::: CAN NOT USE A NON USERCONTROL BASE TYPE FOR SINGLETON LOOKUPS!", LogType.ErrorLog);
                return false;
            }

            // Find the object we need to use for our content.
            var NavigationOutputContent = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.FulcrumSingletons
                ?.FirstOrDefault(SingletonObj => SingletonObj?.SingletonUserControl.GetType() == UserControlType)
                ?.SingletonUserControl;

            // Check if our user control object output is defined or not.
            if (NavigationOutputContent == null) {
                this._navServiceLogger.WriteLog($"[NAVIGATE_TYPE] ::: FAILED TO LOCATE CONTENT FOR TYPE OBJECT: {UserControlType.Name}!", LogType.WarnLog);
                this._navServiceLogger.WriteLog($"[NAVIGATE_TYPE] ::: BUILDING NEW VIEW CONTENT AND STORING NEW VALUES ON INJECTOR CONSTANTS FOR IT NOW...", LogType.WarnLog);

                // Build instance of the type passed in and have it store into the injector constants
                var BuiltSingleton = FulcrumSingletonContent<UserControl, FulcrumViewModelBase>.CreateSingletonInstance(UserControlType, ViewModelType);
                this._navServiceLogger.WriteLog($"[NAVIGATE_TYPE] ::: BUILT NEW OUTPUT CONTENT FOR THE CONTROL TYPE {UserControlType.Name} OK!", LogType.InfoLog);
                this._navServiceLogger.WriteLog($"[NAVIGATE_TYPE] ::: NAVIGATING TO NEW TYPE: {UserControlType.Name}", LogType.TraceLog);
                return this.NavigationFrame.NavigationService?.Content?.GetType() != UserControlType &&
                       this.NavigationFrame.Navigate(BuiltSingleton.SingletonUserControl);
            }

            // Return true if navigation passed correctly
            this._navServiceLogger.WriteLog($"[NAVIGATE_TYPE] ::: NAVIGATING TO NEW TYPE: {UserControlType.Name}", LogType.TraceLog);
            return this.NavigationFrame.NavigationService?.Content?.GetType() != UserControlType &&
                   this.NavigationFrame.Navigate(NavigationOutputContent);
        }

        /// <summary>
        /// Registers a new frame instance onto our navigation helper
        /// </summary>
        private void _registerFrameEvents()
        {
            // Check for frame state value
            if (this._navigationFrame == null) {
                this._navServiceLogger.WriteLog($"[NAVIGATE_REGISTER] ::: FAILED TO REGISTER NEW FRAME SINCE IT WAS NULL!", LogType.TraceLog);
                return;
            }

            // Add new event handlers
            this._navigationFrame.Navigated += this.OnNavigationFrameNavigated;
            this._navigationFrame.NavigationFailed += this.OnNavigationFrameNavigationFailed;
            this._navServiceLogger.WriteLog($"[NAVIGATE_REGISTER] ::: REGISTERED NEW FRAME INSTANCE OBJECT", LogType.TraceLog);
        }
        /// <summary>
        /// Removes a frame instance from our frame object store
        /// </summary>
        private void _unregisterFrameEvents()
        {
            // Check for frame state value
            if (this._navigationFrame == null) {
                this._navServiceLogger.WriteLog($"[NAVIGATE_UNREGISTER] ::: FAILED TO UNREGISTER NEW FRAME SINCE IT WAS NULL!", LogType.TraceLog);
                return;
            }

            // Remove event handlers
            this._navigationFrame.Navigated -= this.OnNavigationFrameNavigated;
            this._navigationFrame.NavigationFailed -= this.OnNavigationFrameNavigationFailed;
            this._navServiceLogger.WriteLog($"[NAVIGATE_UNREGISTER] ::: UNREGISTERED NEW FRAME INSTANCE OBJECT", LogType.TraceLog);
        }
    }
}
