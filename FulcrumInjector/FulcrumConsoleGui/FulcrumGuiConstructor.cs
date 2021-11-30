using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FulcrumInjector.FulcrumJsonHelpers;
using FulcrumInjector.FulcrumLogging;
using FulcrumInjector.FulcrumLogging.LoggerObjects;
using FulcrumInjector.FulcrumLogging.LoggerSupport;
using Terminal.Gui;

namespace FulcrumInjector.FulcrumConsoleGui
{
    /// <summary>
    /// Configures a new FulcrumConsole GUI Instance
    /// </summary>
    public class FulcrumGuiConstructor
    {
        // Logger object 
        private static SubServiceLogger ConsoleLogger => (SubServiceLogger)FulcrumLogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
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
        public static Window ConsoleWindow { get; private set; }
        public static MenuBar ConsoleMenu { get; private set; }

        // -----------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Basic init and configuration for the console configuration
        /// </summary>
        public FulcrumGuiConstructor()
        {
            // Log info about booting session
            Application.Init();
            ConsoleLogger.WriteLog("SETTING UP NEW CONSOLE CONFIGURATION OBJECTS NOW...", LogType.WarnLog);

            // Building new objects for the new console window output.
            ConsoleWindow = this.ConfigureMainWindow();     // Main window view
            ConsoleMenu = this.ConfigureMenuBar();          // Console Menu top bar

            // Append all these objects in here.
            ConsoleLogger.WriteLog("ADDING CONSOLE OBJECTS INTO APP SESSION NOW!", LogType.WarnLog);
            Application.Top.Add(ConsoleMenu, ConsoleWindow);

            // Log done building console objects correctly.
            ConsoleLogger.WriteLog("DONE BUILDING CONSOLE WINDOW AND ALL CHILD OBJECTS!", LogType.InfoLog);
            ConsoleLogger.WriteLog("READY TO RUN THE CONSOLE APPLICATION OBJECT AT ANY TIME TO SHOW FRIENDLY UI VALUES!", LogType.InfoLog);
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
            ConsoleLogger.WriteLog("    --> BUILT NEW TITLE VALUE FOR WINDOW INSTANCE OK!");
            ConsoleLogger.WriteLog($"    --> NEW TITLE VALUE TO STORE: {FullTitleString}", LogType.InfoLog);
            ConsoleLogger.WriteLog($"SET NEW TITLE VALUE FOR WINDOW OBJECT TO: {Console.Title}", LogType.InfoLog);

            // Configure a new window object.
            return ConsoleWindow ?? new Window()
            {
                // Setup window location on the console.
                X = 0,      // Set X and Y spot on the pane.
                Y = 1,      // Set X and Y spot on the pane.

                // Setup window shading
                Width = Dim.Fill(),           // Window filling values
                Height = Dim.Fill()           // Window filling values
            };
        }
        /// <summary>
        /// Builds our new console menu object
        /// </summary>
        private MenuBar ConfigureMenuBar()
        {
            // Build a new menu object
            ConsoleLogger.WriteLog("--> CONFIGURING CONSOLE MENU NOW...", LogType.WarnLog);
            return ConsoleMenu ?? new MenuBar(new MenuBarItem[]
            {
                // Build new menu item named "File" with an option of "Quit" on it
                new MenuBarItem ("_File", new MenuItem []
                {
                    // Menu entry object for Quitting on top of the file dropdown
                    new MenuItem ("_Quit", "", () => { Environment.Exit(0); })
                }),
            });
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
    }
}
