using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FulcrumInjector.AppStyles.AppStyleLogic;
using FulcrumInjector.ViewControl;
using FulcrumInjector.ViewControl.Models;
using MahApps.Metro.Controls;
using NLog;
using NLog.Config;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;
using Path = System.IO.Path;

namespace FulcrumInjector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class InjectorMainWindow : MetroWindow
    {
        // Logger object.
        private static SubServiceLogger InjectorMainLogger => (SubServiceLogger)LogBroker.LoggerQueue.GetLoggers(LoggerActions.SubServiceLogger)
            .FirstOrDefault(LoggerObj => LoggerObj.LoggerName.StartsWith("InjectorMainLogger")) ?? new SubServiceLogger("InjectorMainLogger");

        // --------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds a new main window instance we can use to show our configuration application
        /// </summary>
        public InjectorMainWindow()
        {
            // Init main component and blur background of the main window.
            InitializeComponent();
            this.DataContext = this;   
            App.WindowBlurHelper = new WindowBlurSetup(this, ShowBlur: true);
            InjectorMainLogger.WriteLog("SETUP NEW BLUR EFFECT ON MAIN WINDOW INSTANCE OK!", LogType.InfoLog);
            InjectorMainLogger.WriteLog("WELCOME TO THE FULCRUM INJECTOR. LETS SNIFF SOME CANS", LogType.WarnLog);
        }

        /// <summary>
        /// Configures specific control values when the window is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InjectorMainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Configure the new Logging Output Target.
            var CurrentConfig = LogManager.Configuration;
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("DebugToAvEditRedirect", typeof(DebugLoggingRedirectTarget));
            CurrentConfig.AddRuleForAllLevels(new DebugLoggingRedirectTarget(FulcrumDebugLogging, FulcrumDebugLogging.DebugRedirectOutputEdit));
            LogManager.ReconfigExistingLoggers();

            // Log Added new target output ok
            InjectorMainLogger.WriteLog("INJECTOR HAS REGISTERED OUR DEBUGGING REDIRECT OBJECT OK!", LogType.WarnLog);
            InjectorMainLogger.WriteLog("ALL LOG OUTPUT WILL APPEND TO OUR DEBUG VIEW ALONG WITH THE OUTPUT FILES NOW!", LogType.WarnLog);

            // Store view objects for the UI
            InjectorConstants.ConfigureViewControls(this);
            InjectorMainLogger.WriteLog("STORED UI CONTROLS FOR FLYOUT HELPERS OK!", LogType.InfoLog);

            // Set title to DEBUG if the app is inside our debug directory
            if (Directory.GetCurrentDirectory().Split(Path.DirectorySeparatorChar).Contains("bin")) this.Title += " (SOURCE_BINARY";
#if DEBUG
            if (!this.Title.Contains("(")) this.Title += "("; 
            this.Title += " - DEBUG_BUILD)";
#else
            if (this.Title.Contains("(")) this.Title += ")";
#endif
            // Log information output
            InjectorMainLogger.WriteLog("CONFIGURED NEW TITLE VALUE BASED ON OPERATIONAL CONDITIONS OK!", LogType.InfoLog);
            InjectorMainLogger.WriteLog($"NEW TITLE VALUE CONFIGURED: {this.Title}", LogType.InfoLog);
        }

        // --------------------------------------------------------------------------------------------------------------------------

    }
}
