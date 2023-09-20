﻿using System;
using System.IO;
using System.Linq;
using System.Windows;
using FulcrumInjector.FulcrumViewContent;
using FulcrumInjector.FulcrumViewSupport;
using MahApps.Metro.Controls;
using SharpLogging;

namespace FulcrumInjector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FulcrumMainWindow : MetroWindow
    {
        #region Custom Events
        #endregion //Custom Events

        #region Fields

        // Private backing field for the main window logger
        private readonly SharpLogger _injectorMainLogger;

        #endregion //Fields

        #region Properties
        #endregion //Properties

        #region Structs and Classes
        #endregion //Structs and Classes

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new main window instance we can use to show our configuration application
        /// </summary>
        public FulcrumMainWindow()
        {
            // Init main component and blur background of the main window.
            InitializeComponent();

            // Setup a blur effect for our window background and configure the logger for this window
            FulcrumWindowBlur.ShowBlurEffect(this);
            this._injectorMainLogger = new SharpLogger(LoggerActions.UniversalLogger);
            this._injectorMainLogger.WriteLog("STORED INJECTOR INSTANCE ON OUR CONSTANTS CLASS OK!", LogType.InfoLog);
            this._injectorMainLogger.WriteLog("SETUP NEW BLUR EFFECT ON MAIN WINDOW INSTANCE CORRECTLY!", LogType.InfoLog);
            this._injectorMainLogger.WriteLog("WELCOME TO THE FULCRUM INJECTOR. LETS SNIFF SOME GODDAMN CAN BUS", LogType.WarnLog);
        }

        // ------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Configures specific control values when the window is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InjectorMainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Configure DataContext and setup view controls
            this.DataContext = this;
            FulcrumConstants.FulcrumMainWindow = this;
            
            // Set title to DEBUG if the app is inside our debug directory
            if (Directory.GetCurrentDirectory().Split(Path.DirectorySeparatorChar).Contains("bin")) this.Title += " (SOURCE_BINARY";
#if DEBUG
            if (!this.Title.Contains("(")) this.Title += " (";
            if (!this.Title.EndsWith("(")) this.Title += "-";
            this.Title += "DEBUG_BUILD)";
#else
            if (this.Title.Contains("(")) this.Title += ")";
#endif
            // Log information output
            this._injectorMainLogger.WriteLog("CONFIGURED NEW TITLE VALUE BASED ON OPERATIONAL CONDITIONS OK!", LogType.InfoLog);
            this._injectorMainLogger.WriteLog($"NEW TITLE VALUE CONFIGURED: {this.Title}", LogType.InfoLog);

            // Log Version information output
            this._injectorMainLogger.WriteLog("INJECTOR VERSION INFORMATION BUILT OK!", LogType.InfoLog);
        }   
        /// <summary>
        /// Routine method for closing actions when the main window instance is closed out.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="E"></param>
        private void InjectorMainWindow_OnClosed(object Sender, EventArgs E)
        {
            // Log information about closing out now.
            this._injectorMainLogger.WriteLog("PROCESSED MAIN WINDOW CLOSEOUT ROUTINE CALL! CALLING TERMINATE ROUTINE!", LogType.ErrorLog);
            this._injectorMainLogger.WriteLog("THIS EXIT COMMAND STARTED FROM WITHIN OUR MAIN WINDOW INSTANCE! THIS WAS LIKELY A CLOSE BUTTON CALL", LogType.InfoLog);

            // Invoke the application exit routine now to quit this program
            Application.Current.Shutdown();
        }
    }
}
