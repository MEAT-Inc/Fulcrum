using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumConsoleGui.ConsoleButtonLogic;
using FulcrumInjector.FulcrumJsonHelpers;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Terminal.Gui;

namespace FulcrumInjector.FulcrumConsoleGui
{
    /// <summary>
    /// Configures a new FulcrumConsole GUI Instance
    /// </summary>
    public class FulcrumGuiConstructor
    {
        // Logger object 
        private static SubServiceLogger ConsoleLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("FulcrumGuiLogger")) ?? new SubServiceLogger("FulcrumGuiLogger");

        // Bool to set if the window is open or not.
        private static bool _consoleWindowOpen = false;
        public static bool ConsoleWindowOpen
        {
            get
            {
                // Pull value and return it.
                ConsoleLogger.WriteLog($"PULLED NEW CONSOLE OPEN VALUE OF {_consoleWindowOpen}", LogType.TraceLog);
                return _consoleWindowOpen;
            }
            private set
            {
                // Store value and log it out
                ConsoleLogger.WriteLog($"SETTING NEW CONSOLE WINDOW STATE TO {_consoleWindowOpen}", LogType.TraceLog);
                _consoleWindowOpen = value;
            }
        }

        // Console Window objects used globally.
        public static View[] ConsoleViews => new View[]
        {
            _consoleWindow,      // Main window
            _consoleMenu,        // Menu bar
            _consoleTopPane,     // Top view pane
        };

        // Console components
        private static Window _consoleWindow;
        private static MenuBar _consoleMenu;
        private static FrameView _consoleTopPane;

        // -----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Basic init and configuration for the console configuration
        /// </summary>
        public FulcrumGuiConstructor()
        {
            // Log info about booting session
            ConsoleLogger.WriteLog("SETTING UP NEW CONSOLE CONFIGURATION OBJECTS NOW...", LogType.WarnLog);

            // Building new objects for the new console window output.
            _consoleWindow = this.ConfigureMainWindow();      // Main window view
            _consoleMenu = this.ConfigureMenuBar();           // Console Menu top bar
            _consoleTopPane = this.ConfigureTopInfoPane();    // Top output menu

        }

        /// <summary>
        /// Invokes run of the new console window object and application
        /// </summary>
        public bool ToggleConsoleGuiView()
        {
            // See if we want to toggle or not.
            if (!ValueLoaders.GetConfigValue<bool>("FulcrumConsole.EnableGuiConsole"))
            {
                // Log this output and return false. This should only really happen if debugging is on or something is broken.
                ConsoleLogger.WriteLog("NOT SETTING CONSOLE STATE TO A VALUE! THIS IS BECAUSE OUR BOOLEAN TO ALLOW GUI IS FALSE!", LogType.WarnLog);
                return false;
            }

            try
            {
                // Log info and boot the application
                ConsoleLogger.WriteLog("TOGGLING CONSOLE WINDOW APPLICATION STATE NOW...", LogType.WarnLog);
                if (!ConsoleWindowOpen)
                {
                    // Build application
                    Application.Init();
                    ConsoleLogger.WriteLog("ADDING CONSOLE OBJECTS INTO APP SESSION NOW!", LogType.WarnLog);

                    // Log done building console objects correctly.
                    Application.Top.Add(ConsoleViews);
                    ConsoleLogger.WriteLog("DONE BUILDING CONSOLE WINDOW AND ALL CHILD OBJECTS!", LogType.InfoLog);
                    ConsoleLogger.WriteLog("READY TO RUN THE CONSOLE APPLICATION OBJECT AT ANY TIME TO SHOW FRIENDLY UI VALUES!", LogType.InfoLog);

                    // Run instance here.
                    Application.Run();
                    ConsoleLogger.WriteLog("BOOTED NEW CONSOLE APPLICATION INSTANCE OK!", LogType.InfoLog);

                    // Return startup passed ok and toggle the state boolean
                    ConsoleWindowOpen = true;
                    return true;
                }

                // Close down the instance if it's currently open.
                ConsoleLogger.WriteLog("CLOSING EXISTING WINDOW OBJECT ON THE CONSOLE INSTANCE...", LogType.WarnLog);
                Application.RequestStop();

                // Return shutdown passed ok and toggle the state boolean
                ConsoleWindowOpen = false;
                return true;
            }
            catch (Exception RunEx)
            {
                // Log failures.
                ConsoleLogger.WriteLog("FAILED TO CONFIGURE NEW CONSOLE SESSION OBJECTS!", LogType.ErrorLog);
                ConsoleLogger.WriteLog("FAILED DURING RUN INVOCATION OF THE CONSOLE WINDOW!", RunEx);
                return false;
            }
        }

        #region View Generation Routines
        /// <summary>
        /// Builds our new console menu object
        /// </summary>
        private MenuBar ConfigureMenuBar()
        {
            // Build a new menu object
            ConsoleLogger.WriteLog("--> CONFIGURING CONSOLE MENU NOW...", LogType.WarnLog);
            return _consoleMenu ?? new MenuBar(new MenuBarItem[]
            {
                // File menu entry items
                new MenuBarItem ("_File", new MenuItem [] {
                    new MenuItem ("_Quit", "", () => { ConsoleButtonActions.ConsoleAppExit(0); }),
                }),

                // View menu entry items
                new MenuBarItem("_View", new MenuItem[] {
                    new MenuItem("_Log Files", "",  ConsoleButtonActions.ShowLogFilesPopup),
                }),

                // About menu with a dropdown for information about this application
                new MenuBarItem("_About", new MenuItem[] {
                    new MenuItem("_Support", "", ConsoleButtonActions.ShowHelpPopup),
                    new MenuItem("_Version", "", ConsoleButtonActions.ShowVersionPopup),
                })
            });
        }
        /// <summary>
        /// Builds a new left hand pane for the frame view output
        /// </summary>
        /// <returns>Left hand pane object</returns>
        private FrameView ConfigureTopInfoPane()
        {
            // Log information and build new pane
            ConsoleLogger.WriteLog("--> CONFIGURING CONSOLE LEFT HAND PANE NOW...", LogType.WarnLog);
            var LeftPaneView = new FrameView("FulcrumInjector - Application Status")
            {
                X = 0,                              // Starting at 0
                Y = 1,                              // For Left Side Menu
                Width = Dim.Fill(),                 // Fill width
                Height = 10,                        // 10 tall
                CanFocus = false,                   // Not focusable
                Shortcut = Key.CtrlMask | Key.C     // Focus shortcut
            };

            // Apply title value and shortcut values
            LeftPaneView.ShortcutAction = () => LeftPaneView.SetFocus();
            ConsoleLogger.WriteLog("--> BUILT NEW PANE VIEW OK! SHORTCUT AND TITLE HAVE BEEN APPLIED TO IT WITHOUT ISSUES!", LogType.TraceLog);

            // Return new view built.
            return LeftPaneView;
        }
        /// <summary>
        /// Builds and returns a new console window object.
        /// </summary>
        /// <returns></returns>
        private Window ConfigureMainWindow()
        {
            // Log starting to build window.
            ConsoleLogger.WriteLog("--> CONFIGURING NEW CONSOLE WINDOW OBJECT NOW...", LogType.WarnLog);

            // Pull the app title value here.
            string CurrentAppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string AppName = ValueLoaders.GetConfigValue<string>("AppInstanceName");
            string FullTitleString = $"{AppName} -- Version {CurrentAppVersion}";

            // Set window title to contain the version of the application instance.
            Console.Title = $"{AppName} -- Version {CurrentAppVersion}";
            ConsoleLogger.WriteLog("--> BUILT NEW TITLE VALUE FOR WINDOW INSTANCE OK!");
            ConsoleLogger.WriteLog($"--> NEW TITLE VALUE TO STORE: {FullTitleString}", LogType.InfoLog);
            ConsoleLogger.WriteLog($"SET NEW TITLE VALUE FOR WINDOW OBJECT TO: {Console.Title}", LogType.InfoLog);

            // Configure a new window object.
            return _consoleWindow ?? new Window()
            {
                // Setup window location on the console.
                X = 0,     // Start X value at 0.
                Y = 11,    // Start Y Value at 11 (One under the top menu pane value)

                // Setup window shading
                Width = Dim.Fill(),           // Window filling values
                Height = Dim.Fill()           // Window filling values
            };
        }
        #endregion
    }
}
