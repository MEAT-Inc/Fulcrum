using System;
using System.Collections.Generic;
using System.Linq;
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
using MahApps.Metro.Controls;
using SharpLogger;
using SharpLogger.LoggerObjects;
using SharpLogger.LoggerSupport;

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
        /// Executes the logic setup for this app once our window instance is opened.
        /// </summary>
        private void InjectorMainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Log info about this method
            InjectorConstants.ConfigureViewControls(this);
            InjectorMainLogger.WriteLog("WINDOW OBJECT HAS BEEN LOADED AND OPENED! SETTING UP LOGIC NOW...", LogType.InfoLog);
            InjectorMainLogger.WriteLog("ONCE UI CONTENT IS STATIC, PIPE LOGIC AND OTHER BUILT OBJECTS WILL POPULATE...");

            // Store constants in here.
            Task.Run(() =>
            {
                InjectorConstants.ConfigureFulcrumPipes();
                InjectorMainLogger.WriteLog("FULCRUM PIPE CONFIGURATION HAS BEEN COMPLETED. CHECK THE UI AND LOG FILES FOR RESULTS", LogType.WarnLog);
                InjectorMainLogger.WriteLog("AT THIS POINT IF THE CALLS PASSED, OUR APP IS READY TO PROCESS J2534 SHIMMED CALLS!", LogType.InfoLog);
            });
        }
    }
}
